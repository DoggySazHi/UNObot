cmake_minimum_required (VERSION 3.8)

project ("RCONHelper")

file(GLOB RCONHelper_SRC
        "src/RCONHelper/*.h"
        "src/RCONHelper/*.cpp"
        "src/RCONHelper/*.c"
        )

if(CMAKE_BUILD_TYPE MATCHES Debug)
    target_compile_definitions(RCONHelper PUBLIC DEBUG=1)
elseif(CMAKE_BUILD_TYPE MATCHES Release)
    target_compile_definitions(RCONHelper PUBLIC DEBUG=0)
endif()

add_library(RCONHelper SHARED ${RCONHelper_SRC})