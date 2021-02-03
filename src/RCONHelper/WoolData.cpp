#include <vector>
#include "WoolData.h"
#include "RCONHelper.h"

std::vector<Wool> WoolData::colors;
int counter = 0;
#define FULL_COLOR

WoolData::WoolData() {
    if (colors.empty()) {
        colors.push_back(Wool{0, 233, 236, 236, "white_wool"}); // White
#ifdef FULL_COLOR
        colors.push_back(Wool{1, 240, 118, 19, "orange_wool"}); // Orange
        colors.push_back(Wool{2, 189, 68, 179, "magenta_wool"}); // Magenta
        colors.push_back(Wool{3, 58, 175, 217, "light_blue_wool"}); // Light Blue
        colors.push_back(Wool{4, 248, 198, 39, "yellow_wool"}); // Yellow
        colors.push_back(Wool{5, 112, 185, 25, "lime_wool"}); // Lime
        colors.push_back(Wool{6, 237, 141, 17, "pink_wool"}); // Pink
        colors.push_back(Wool{7, 62, 68, 71, "gray_wool"}); // Gray
        colors.push_back(Wool{8, 142, 142, 134, "light_gray_wool"}); // Light Gray
        colors.push_back(Wool{9, 21, 137, 145, "cyan_wool"}); // Cyan
        colors.push_back(Wool{10, 121, 42, 172, "purple_wool"}); // Purple
        colors.push_back(Wool{11, 53, 57, 157, "blue_wool"}); // Blue
        colors.push_back(Wool{12, 114, 71, 40, "brown_wool"}); // Brown
        colors.push_back(Wool{13, 84, 109, 27, "green_wool"}); // Green
        colors.push_back(Wool{14, 161, 39, 34, "red_wool"}); // Red
#endif
        colors.push_back(Wool{15, 20, 21, 25, "black_wool"}); // Black
    }
    command.reserve(100);
}

Wool* WoolData::getClosestColor(int r, int g, int b) {
    Wool* closest = &colors[0];
    int min = 0x7FFFFFFF;

    for(auto& w : colors) {
        // multiplication for "optimization", also offsets for color perception
        int rr = w.color[0], gg = w.color[1], bb = w.color[2];

        auto d = (rr - r) * (rr - r) * 3 + (gg - g) * (gg - g) * 6 + (bb - b) * (bb - b) * 1;
        counter++;
        if (d < min) {
            min = d;
            closest = &w;
        }
    }
    return closest;
}

std::vector<uint8_t> WoolData::getPayload(int x, int y, Wool* sheep) {

    command.clear();
    command += "setblock ";
    command += std::to_string(x);
    command += " 4 -";
    command += std::to_string(y);
    command += " ";
    command += sheep->blockName;


    auto payload = RCONSocket::MakePacketData(command, SERVERDATA_EXECCOMMAND, 0);
    return payload;
}