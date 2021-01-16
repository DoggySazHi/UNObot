#include <string>
#include <vector>

struct Wool {
    unsigned char id;
    unsigned char color[3]; // put in RGB, not BGR
};

class WoolData {
public:
    WoolData();
    static unsigned char getClosestColor(unsigned char r, unsigned char g, unsigned char b);
    std::vector<uint8_t> &getPayload(int x, int y, unsigned char sheep);
private:
    static std::vector<Wool> colors; // static to avoid excess dictionaries
    std::vector<uint8_t> templatePayload;
};