obj-m += kpaste.o

ccflags-y += -Wno-declaration-after-statement

all:
	make -C /lib/modules/$(shell uname -r)/build M=$(PWD) modules

clean:
	make -C /lib/modules/$(shell uname -r)/build M=$(PWD) clean

run: all
	sudo dmesg -c >/dev/null
	sudo rmmod hello || true
	sudo insmod hello.ko
	sudo dmesg -c
