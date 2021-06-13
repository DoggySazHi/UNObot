#include <string>
#include <vector>
#include <unordered_map>

struct Instrument {
    std::string name;
    int modifier;
};

class NoteData {
public:
    NoteData();
    std::vector<uint8_t> getPayload(int instrument, int pitch, float velocity, int channel);
private:
    static std::unordered_map<int, Instrument> instruments;
    static std::unordered_map<int, Instrument> percussion;
    static std::pair<float, std::string> computePitch(int instrument, int channel, int pitch);
    std::string command;
};