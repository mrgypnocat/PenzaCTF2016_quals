#include <rands.h>

void hash(unsigned char *buf, int buflen)
{
        buf[0] ^= RAND_CHAR_1;
        buf[1] ^= RAND_CHAR_2;
        buf[2] ^= RAND_CHAR_3;
        buf[3] ^= RAND_CHAR_4;

        for (int i = 0; i < buflen; ++i) {
                buf[i] ^= RAND_CHAR_17;
        }

        buf[4] ^= RAND_CHAR_5;
        buf[5] ^= RAND_CHAR_6;
        buf[6] ^= RAND_CHAR_7;
        buf[7] ^= RAND_CHAR_8;
        buf[8] ^= RAND_CHAR_9;
        buf[9] ^= RAND_CHAR_10;

        for (int i = 0; i < buflen; ++i) {
                buf[i] ^= RAND_CHAR_18;
        }

        buf[10] ^= RAND_CHAR_11;
        buf[11] ^= RAND_CHAR_12;
        buf[12] ^= RAND_CHAR_13;
        buf[13] ^= RAND_CHAR_14;
        buf[14] ^= RAND_CHAR_15;
        buf[15] ^= RAND_CHAR_16;
}
