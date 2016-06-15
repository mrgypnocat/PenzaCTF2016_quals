#include <stdio.h>

#include <hashedflag.h>

void hash(unsigned char *buf, int buflen);

int main(void)
{
        const int buflen = sizeof(hashed_buf);

        unsigned char b[buflen];

        printf("Write flag: ");

        scanf("%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x",
              &b[0], &b[1], &b[2], &b[3], &b[4], &b[5], &b[6], &b[7], &b[8],
              &b[9], &b[10], &b[11], &b[12], &b[13], &b[14], &b[15]);

        hash(b, buflen);

        if (!memcmp(hashed_buf, b, buflen)) {
                puts("You win!");
        } else {
                puts("You fail :(");
        }

        return 0;
}
