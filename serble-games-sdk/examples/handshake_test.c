#include "../serble_games.h"
#include <stdio.h>

int main(int argc, char **argv) {
    if (argc < 2) {
        fprintf(stderr, "Usage: %s <game_id> [achievement_id]\n", argv[0]);
        return 1;
    }

    if (argc >= 3) {
        return serble_games_grant_achievement(argv[1], argv[2]);
    }

    return serble_games_init(argv[1]);
}