#include <string>
#include <vector>

struct Wool {
    unsigned char id;
    int color[3]; // put in RGB, not BGR
    std::string blockName;
};

class WoolData {
public:
    WoolData();
    static Wool* getClosestColor(int r, int g, int b);
    std::vector<uint8_t> getPayload(int x, int y, Wool* sheep);
private:
    static std::vector<Wool> colors; // static to avoid excess dictionaries
    std::string command;
};