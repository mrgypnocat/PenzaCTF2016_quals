#include <stdio.h>

#include <flag.h>

void hash(unsigned char *buf, int buflen);

int main(void)
{
        hash(buf, buflen);

	printf("const char hashed_buf[] = { ");
        for (int i = 0; i < buflen; ++i) {
                printf("0x%02x, ", buf[i] & 0xFF);
        }
        printf("};\n");

        return 0;
}
