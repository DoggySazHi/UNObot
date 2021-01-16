#include <cstdint>
#include <cstdio>
#include <cctype>
#include <fstream>

class Utilities {
public:
    static inline bool is_big_endian() {
        union {
            uint32_t i;
            char c[4];
        } bint = {0x01020304};

        return bint.c[0] == 1;
    }

    static inline void hexdump(void *ptr, int buflen) {
        auto *buf = (unsigned char *) ptr;
        int i, j;
        for (i = 0; i < buflen; i += 16) {
            printf("%06x: ", i);
            for (j = 0; j < 16; j++)
                if (i + j < buflen)
                    printf("%02x ", buf[i + j]);
                else
                    printf("   ");
            printf(" ");
            for (j = 0; j < 16; j++)
                if (i + j < buflen)
                    printf("%c", isprint(buf[i + j]) ? buf[i + j] : '.');
            printf("\n");
        }
    }

    static inline void file_dump(void *ptr, int buflen, const std::string &name) {
        auto *buf = (char *) ptr;
        std::ofstream file;
        file.open(name, std::ios::binary);
        file.write(buf, buflen);
        file.close();
    }
};