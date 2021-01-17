#include "MIDIHelper.h"
#include "RCONHelper.h"
#include <iostream>
#include "MidiFile.h"

volatile int MIDIHelper::idCounter = 0;

MIDIHelper::MIDIHelper() : id(0) {
    startThread();
    smf::MidiFile midi;
    midi.read("lol");
    midi.doTimeAnalysis();
    midi.linkNotePairs();
    midi.linkEventPairs();
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

    while (!cancellationToken.stop_requested()){
        signalToThread.wait(cancellationToken);
        if (cancellationToken.stop_requested())
            break;
        // Work!
        signalToMain.notify();
    }

    RCONHelper::SayDelete(reinterpret_cast<const char *>(rcon));
    std::cout << "Ended thread #" << id << "!\n";
}