cmake_minimum_required(VERSION 4.1)
project(serble_games_sdk C)

set(CMAKE_C_STANDARD 11)

add_library(serble_games_sdk STATIC
        serble_games.c
        serble_games.h)

target_include_directories(serble_games_sdk PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

if (WIN32)
    target_link_libraries(serble_games_sdk PUBLIC ws2_32)
endif()

option(SERBLE_GAMES_SDK_BUILD_EXAMPLE "Build handshake example" OFF)
if (SERBLE_GAMES_SDK_BUILD_EXAMPLE)
    add_executable(serble_games_handshake_example examples/handshake_test.c)
    target_link_libraries(serble_games_handshake_example PRIVATE serble_games_sdk)
endif()