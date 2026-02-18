#ifndef SERBLE_GAMES_SDK_SERBLE_GAMES_H
#define SERBLE_GAMES_SDK_SERBLE_GAMES_H

int serble_games_init(const char *game_id);
int serble_games_grant_achievement(const char *game_id, const char *achievement_id);
int serble_games_sdk_init(const char *game_id);

#endif //SERBLE_GAMES_SDK_SERBLE_GAMES_H