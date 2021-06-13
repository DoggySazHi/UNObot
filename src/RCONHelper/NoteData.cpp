#include <vector>
#include "NoteData.h"
#include "RCONHelper.h"
#include <cmath>

std::unordered_map<int, Instrument> NoteData::instruments;
std::unordered_map<int, Instrument> NoteData::percussion;

NoteData::NoteData() {
    if (instruments.empty()) {
        instruments[24] = Instrument{"minecraft:block.note_block.harp", -30 - 12 - 24};

        instruments[81] = Instrument{"minecraft:block.note_block.harp", -30 - 12 - 24};
        instruments[82] = Instrument{"minecraft:block.note_block.harp", -30 - 12 - 24};
        instruments[111] = Instrument{"minecraft:block.note_block.harp", -30 - 12 - 24};
        instruments[41] = Instrument{"minecraft:block.note_block.flute", -42 - 12};
        instruments[30] = Instrument{"minecraft:block.note_block.guitar", -18 - 12};
        instruments[31] = Instrument{"minecraft:block.note_block.guitar", -18 - 12};
    }
    if (percussion.empty()) {
        percussion[35] = Instrument{"minecraft:block.note_block.bass", -30 - 12};
        percussion[36] = Instrument{"minecraft:block.note_block.bass", -30 - 12};
        percussion[37] = Instrument{"minecraft:block.note_block.hat", -30 - 12};
        percussion[40] = Instrument{"minecraft:block.note_block.snare", -30 - 4};
        percussion[42] = Instrument{"minecraft:block.note_block.snare", -30 - 4};
        percussion[43] = Instrument{"minecraft:block.note_block.bass", -30 - 12};
        percussion[44] = Instrument{"minecraft:block.note_block.snare", -30 - 12};
        percussion[45] = Instrument{"minecraft:block.note_block.bass", -30 - 12};
        percussion[48] = Instrument{"minecraft:block.note_block.bass", -30 - 12};
        percussion[49] = Instrument{"minecraft:block.note_block.snare", -30 - 4};
        percussion[50] = Instrument{"minecraft:block.note_block.bass", -30 - 12};
        percussion[57] = Instrument{"minecraft:block.note_block.snare", -30 - 4};
        percussion[62] = Instrument{"minecraft:block.note_block.hat", -30 - 12};
        percussion[77] = Instrument{"minecraft:block.note_block.hat", -30 - 12};
    }
    command.reserve(100);
}

std::vector<uint8_t> NoteData::getPayload(int instrument, int pitch, float velocity, int channel) {
    command.clear();
    auto noteData = computePitch(instrument, channel, pitch);
    command += "execute as @a at @s run playsound ";
    command += noteData.second;
    command += " record @s ^0 ^ ^ ";
    auto velocityFloat = std::to_string(velocity);
    command += velocityFloat.substr(0, std::min(velocityFloat.find('.') + 4, velocityFloat.length()));
    command += " ";
    auto pitchFloat = std::to_string(noteData.first);
    pitchFloat = pitchFloat.substr(0, std::min(pitchFloat.find('.') + 4, pitchFloat.length()));
    command += pitchFloat;
    command += " 1";
    auto packet = RCONSocket::MakePacketData(command, SERVERDATA_EXECCOMMAND, 0);
    return packet;
}

std::pair<float, std::string> NoteData::computePitch(int instrument, int channel, int pitch) {
    Instrument* item;
    if (channel == 10)
        item = &percussion[instrument + 33];
    else
        item = &instruments[instrument + 1];

    std::string actual_instrument = std::string(item->name);
    pitch += item->modifier;

    //int numerator = (int) pitch - 12;
    int numerator = pitch;
    if (numerator > 12) {
        numerator -= 24;
        actual_instrument += "_1";
    }
    if (numerator < -12) {
        numerator += 24;
        actual_instrument += "_-1";
    }

    auto output = (float) pow(2, (float) (numerator) / 12.0f);
    return std::pair<float, std::string>(output, actual_instrument);
}
