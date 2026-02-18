#include "serble_games.h"
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")
typedef SOCKET sg_socket_t;
#define SG_INVALID_SOCKET INVALID_SOCKET
#define sg_close closesocket
#else
#include <sys/types.h>
#include <sys/socket.h>
#include <netdb.h>
#include <unistd.h>
typedef int sg_socket_t;
#define SG_INVALID_SOCKET (-1)
#define sg_close close
#endif

enum {
    SG_PACKET_HANDSHAKE = 0,
    SG_PACKET_HANDSHAKE_RESPONSE = 1,
    SG_PACKET_ACK = 2,
    SG_PACKET_GRANT_ACHIEVEMENT = 3
};

#define MANAGEMENT_PORT "46599"

static void sg_write_int32_be(uint8_t *out, int32_t value) {
    out[0] = (uint8_t)((value >> 24) & 0xff);
    out[1] = (uint8_t)((value >> 16) & 0xff);
    out[2] = (uint8_t)((value >> 8) & 0xff);
    out[3] = (uint8_t)(value & 0xff);
}

static int32_t sg_read_int32_be(const uint8_t *in) {
    return (int32_t)(((uint32_t)in[0] << 24) | ((uint32_t)in[1] << 16) | ((uint32_t)in[2] << 8) | (uint32_t)in[3]);
}

static int sg_write_all(sg_socket_t socket_fd, const uint8_t *data, size_t length) {
    size_t sent = 0;
    while (sent < length) {
#ifdef _WIN32
        int result = send(socket_fd, (const char *)(data + sent), (int)(length - sent), 0);
#else
        ssize_t result = send(socket_fd, data + sent, length - sent, 0);
#endif
        if (result <= 0) {
            return 1;
        }
        sent += (size_t)result;
    }
    return 0;
}

static int sg_read_all(sg_socket_t socket_fd, uint8_t *data, size_t length) {
    size_t received = 0;
    while (received < length) {
#ifdef _WIN32
        int result = recv(socket_fd, (char *)(data + received), (int)(length - received), 0);
#else
        ssize_t result = recv(socket_fd, data + received, length - received, 0);
#endif
        if (result <= 0) {
            return 1;
        }
        received += (size_t)result;
    }
    return 0;
}

// ...existing code...
#ifdef _WIN32
static int sg_wsa_startup(void) {
    WSADATA wsa_data;
    return WSAStartup(MAKEWORD(2, 2), &wsa_data) == 0 ? 0 : 1;
}
#endif

static void sg_cleanup_socket(sg_socket_t socket_fd, int wsa_started) {
    if (socket_fd != SG_INVALID_SOCKET) {
        sg_close(socket_fd);
    }
#ifdef _WIN32
    if (wsa_started) {
        WSACleanup();
    }
#endif
}

static int sg_open_socket(sg_socket_t *out_socket) {
    struct addrinfo hints;
    struct addrinfo *result = NULL;
    memset(&hints, 0, sizeof(hints));
    hints.ai_family = AF_INET;
    hints.ai_socktype = SOCK_STREAM;

    if (getaddrinfo("127.0.0.1", "46599", &hints, &result) != 0) {
        return 1;
    }

    sg_socket_t socket_fd = SG_INVALID_SOCKET;
    for (struct addrinfo *addr = result; addr != NULL; addr = addr->ai_next) {
        socket_fd = socket(addr->ai_family, addr->ai_socktype, addr->ai_protocol);
        if (socket_fd == SG_INVALID_SOCKET) {
            continue;
        }
        if (connect(socket_fd, addr->ai_addr, (int)addr->ai_addrlen) == 0) {
            break;
        }
        sg_close(socket_fd);
        socket_fd = SG_INVALID_SOCKET;
    }

    freeaddrinfo(result);

    if (socket_fd == SG_INVALID_SOCKET) {
        return 1;
    }

    *out_socket = socket_fd;
    return 0;
}

static int sg_send_string_packet(sg_socket_t socket_fd, int32_t type, const char *value) {
    int32_t value_len = (int32_t)strlen(value);
    int32_t data_len = 4 + value_len;
    int32_t packet_len = 4 + data_len;
    size_t total_len = 4 + (size_t)packet_len;

    uint8_t *packet = (uint8_t *)malloc(total_len);
    if (!packet) {
        return 1;
    }

    sg_write_int32_be(packet, packet_len);
    sg_write_int32_be(packet + 4, type);
    sg_write_int32_be(packet + 8, value_len);
    if (value_len > 0) {
        memcpy(packet + 12, value, (size_t)value_len);
    }

    int result = sg_write_all(socket_fd, packet, total_len);
    free(packet);
    return result;
}

static int sg_read_packet_type(sg_socket_t socket_fd, int32_t *out_type) {
    uint8_t header[4];
    if (sg_read_all(socket_fd, header, sizeof(header)) != 0) {
        return 1;
    }

    int32_t response_len = sg_read_int32_be(header);
    if (response_len < 4) {
        return 1;
    }

    uint8_t *response = (uint8_t *)malloc((size_t)response_len);
    if (!response) {
        return 1;
    }

    if (sg_read_all(socket_fd, response, (size_t)response_len) != 0) {
        free(response);
        return 1;
    }

    *out_type = sg_read_int32_be(response);
    free(response);
    return 0;
}

static int sg_perform_handshake(sg_socket_t socket_fd, const char *game_id) {
    if (sg_send_string_packet(socket_fd, SG_PACKET_HANDSHAKE, game_id) != 0) {
        return 1;
    }

    int32_t response_type = 0;
    if (sg_read_packet_type(socket_fd, &response_type) != 0) {
        return 1;
    }

    return response_type == SG_PACKET_HANDSHAKE_RESPONSE ? 0 : 1;
}

int serble_games_init(const char *game_id) {
    if (!game_id || game_id[0] == '\0') {
        return 1;
    }

#ifdef _WIN32
    int wsa_started = sg_wsa_startup() == 0 ? 1 : 0;
    if (!wsa_started) {
        return 1;
    }
#else
    int wsa_started = 0;
#endif

    sg_socket_t socket_fd = SG_INVALID_SOCKET;
    if (sg_open_socket(&socket_fd) != 0) {
        sg_cleanup_socket(socket_fd, wsa_started);
        return 1;
    }

    int result = sg_perform_handshake(socket_fd, game_id);
    sg_cleanup_socket(socket_fd, wsa_started);
    return result;
}

int serble_games_grant_achievement(const char *game_id, const char *achievement_id) {
    if (!game_id || game_id[0] == '\0' || !achievement_id || achievement_id[0] == '\0') {
        return 1;
    }

#ifdef _WIN32
    int wsa_started = sg_wsa_startup() == 0 ? 1 : 0;
    if (!wsa_started) {
        return 1;
    }
#else
    int wsa_started = 0;
#endif

    sg_socket_t socket_fd = SG_INVALID_SOCKET;
    if (sg_open_socket(&socket_fd) != 0) {
        sg_cleanup_socket(socket_fd, wsa_started);
        return 1;
    }

    if (sg_perform_handshake(socket_fd, game_id) != 0) {
        sg_cleanup_socket(socket_fd, wsa_started);
        return 1;
    }

    if (sg_send_string_packet(socket_fd, SG_PACKET_GRANT_ACHIEVEMENT, achievement_id) != 0) {
        sg_cleanup_socket(socket_fd, wsa_started);
        return 1;
    }

    int32_t response_type = 0;
    if (sg_read_packet_type(socket_fd, &response_type) != 0) {
        sg_cleanup_socket(socket_fd, wsa_started);
        return 1;
    }

    sg_cleanup_socket(socket_fd, wsa_started);
    return response_type == SG_PACKET_ACK ? 0 : 1;
}

int serble_games_sdk_init(const char *game_id) {
    return serble_games_init(game_id);
}