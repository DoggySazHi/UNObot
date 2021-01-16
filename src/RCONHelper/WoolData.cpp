#include <vector>
#include "WoolData.h"
#include "RCONHelper.h"

std::vector<Wool> WoolData::colors;

WoolData::WoolData() {
    if (colors.empty()) {
        colors.push_back(Wool{0, 233, 236, 236}); // White
        colors.push_back(Wool{15, 20, 21, 25}); // Black
#ifdef FULL_COLOR
        colors.push_back(Wool{1, 240, 118, 19}); // Orange
        colors.push_back(Wool{2, 189, 68, 179}); // Magenta
        colors.push_back(Wool{3, 58, 175, 217}); // Cyan
        colors.push_back(Wool{4, 248, 198, 39}); // Yellow
        colors.push_back(Wool{5, 112, 185, 25}); // Lime
        colors.push_back(Wool{6, 237, 141, 172}); // Pink
        colors.push_back(Wool{7, 62, 68, 71}); // Gray
        colors.push_back(Wool{8, 142, 142, 134}); // Light Gray
        colors.push_back(Wool{9, 21, 137, 145}); // Cyan
        colors.push_back(Wool{10, 121, 42, 172}); // Purple
        colors.push_back(Wool{11, 53, 57, 157}); // Blue
        colors.push_back(Wool{12, 114, 71, 40}); // Brown
        colors.push_back(Wool{13, 84, 109, 27}); // Green
        colors.push_back(Wool{14, 161, 39, 34}); // Red
#endif
    }
    std::string templateCommand = "execute as @e[type=sheep,x=000.50,z=000.50,r=1] at @s run data modify entity @s Color set value 00";
    templatePayload = RCONSocket::MakePacketData(templateCommand, SERVERDATA_EXECCOMMAND, 0);
}

unsigned char WoolData::getClosestColor(unsigned char r, unsigned char g, unsigned char b) {
    unsigned char closest = 0;
    // float min = std::numeric_limits<float>::infinity();
    int min = 0x7FFFFFFF;

    for(auto& w : colors) {
        // multiplication for "optimization", also offsets for color perception

        auto d = ((w.color[0] - r) * 3) * ((w.color[0]-r) * 3)
                 + ((w.color[1] - g) * 6) * ((w.color[1]-g) * 6)
                 + ((w.color[2] - b) * 1) * ((w.color[2]-b) * 1);

        if (d < min) {
            min = d;
            closest = w.id;
        }
    }

    return closest;
}

std::vector<uint8_t> &WoolData::getPayload(int x, int y, unsigned char sheep) {
    templatePayload[39] = '0' + x / 100;
    templatePayload[40] = '0' + (x / 10) % 10;
    templatePayload[41] = '0' + x % 10;

    templatePayload[48] = '0' + y / 100;
    templatePayload[49] = '0' + (y / 10) % 10;
    templatePayload[50] = '0' + y % 10;

    templatePayload[108] = '0' + sheep / 10;
    templatePayload[109] = '0' + sheep % 10;

    return templatePayload;
}