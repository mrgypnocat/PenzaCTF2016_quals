#include <rands.h>

void hash(unsigned char *buf, int buflen) {
        for (int i = 0; i < buflen; ++i) {
                buf[i] ^= RAND_CHAR_1;
        }
}
