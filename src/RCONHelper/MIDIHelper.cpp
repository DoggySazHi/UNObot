#include "MIDIHelper.h"
#include "RCONHelper.h"
#include <iostream>
#include <unordered_map>
#include "MidiFile.h"
#include "NoteData.h"

volatile int MIDIHelper::idCounter = 0;

MIDIHelper::MIDIHelper(std::string& fileName) : fileName(fileName), id(0) {
    startThread();
}

MIDIHelper::~MIDIHelper() {
    thread.request_stop();
    signalToThread.notify();
    std::cout << "Destroying thread #" << id << "!\n";
}

void MIDIHelper::startThread() {
    thread = std::jthread([this](const std::stop_token& cancellationToken){
        id = idCounter;
        idCounter = idCounter + 1;
        std::cout << "Started thread #" << id << "!\n";
        MIDIHelper::threadMain(cancellationToken);
    });
}

void MIDIHelper::threadMain(const std::stop_token& cancellationToken) {
    const char* ip = "127.0.0.1";
    unsigned short port = 25575;
    const char* password = "mukyumukyu";
    auto rcon = RCONHelper::CreateObjectB(ip, port, password);

    NoteData nd;
    smf::MidiFile midi;
    midi.read("/tmp/tmp.FlC7Zw1mO9/nomico_bad_apple_decode.mid");
    midi.doTimeAnalysis();
    midi.linkNotePairs();
    midi.linkEventPairs();
    midi.joinTracks();

    std::unordered_map<int, int> currentInstruments;

    //std::this_thread::sleep_for(std::chrono::milliseconds(1200));
    auto startTime = std::chrono::steady_clock::now();
    int lastNote = 0;
    while (!cancellationToken.stop_requested()){
        signalToThread.wait(cancellationToken);
        if (cancellationToken.stop_requested())
            break;
        // Work!
        if(midi.getTrackCount() == 0 || midi[0].getSize() == 0)
            break;
        auto timeDiff = std::chrono::duration_cast<std::chrono::duration<double, std::ratio<1, 1>>>(std::chrono::steady_clock::now() - startTime);

        while(lastNote < midi[0].getSize() && midi[0][lastNote].seconds <= timeDiff.count()) {
            auto note = midi[0][lastNote];
            if (note.isTimbre())
                currentInstruments.insert_or_assign(note.track, note.getP1());
            else if (note.isNote()){
                auto instrument = currentInstruments[note.track];
                float velocity = (float) note.getVelocity() / 127.0f;
                auto key = note.getP1();
                auto command = nd.getPayload(instrument, key, velocity, note.getChannel() + 1);
                rcon->ExecuteFast(command);
            }
            lastNote++;
        }
        signalToMain.notify();
    }

    signalToThread.dispose();
    signalToMain.dispose();
    RCONHelper::SayDelete(reinterpret_cast<const char *>(rcon));
    std::cout << "Ended thread #" << id << "!\n";
}