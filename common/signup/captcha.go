package main

import (
	"fmt"
	"io"
	"io/ioutil"
	"math/rand"
	"os"
	"os/exec"
	"time"
)

func readBytesUntilEOF(pipe io.ReadCloser) (buf []byte, err error) {

	bufSize := 1024

	for err != io.EOF {
		stdout := make([]byte, bufSize)
		var n int

		n, err = pipe.Read(stdout)
		if err != nil && err != io.EOF {
			return
		}

		buf = append(buf, stdout[:n]...)
	}

	if err == io.EOF {
		err = nil
	}

	return
}

func readUntilEOF(pipe io.ReadCloser) (str string, err error) {
	buf, err := readBytesUntilEOF(pipe)
	str = string(buf)
	return
}

func system(name string, arg ...string) (stdout string, stderr string,
	err error) {

	cmd := exec.Command(name, arg...)

	outPipe, err := cmd.StdoutPipe()
	if err != nil {
		return
	}

	errPipe, err := cmd.StderrPipe()
	if err != nil {
		return
	}

	cmd.Start()

	stdout, err = readUntilEOF(outPipe)
	if err != nil {
		return
	}

	stderr, err = readUntilEOF(errPipe)
	if err != nil {
		return
	}

	err = cmd.Wait()

	return
}

func makeRandsHeader(filename string, count int) (err error) {
	f, err := os.Create(filename)
	if err != nil {
		return
	}
	defer f.Close()

	for i := 0; i < count; i++ {
		fmt.Fprintf(f, "#define RAND_CHAR_%d %d\n", i, rand.Intn(255))
		fmt.Fprintf(f, "#define RAND_INT_%d %d\n", i, rand.Int31())
		fmt.Fprintf(f, "#define RAND_LONG_%d %d\n", i, rand.Int63())
	}

	return
}

func generateFlag() (flag []byte, err error) {
	flag = make([]byte, 16)
	_, err = rand.Read(flag)
	if err != nil {
		return
	}

	return
}

func makeFlagHeader(filename string, flag []byte) (err error) {
	f, err := os.Create(filename)
	if err != nil {
		return
	}
	defer f.Close()

	fmt.Fprintf(f, "const int buflen = %d;\n", len(flag))

	fmt.Fprintf(f, "char buf[] = { ")
	for i := 0; i < len(flag); i++ {
		fmt.Fprintf(f, "0x%02x, ", flag[i])
	}
	fmt.Fprintf(f, "};\n")

	return
}

func makeCaptcha(workdir, csource string) (flag, path string, err error) {
	rand.Seed(time.Now().UnixNano())

	path = workdir + "/crackme"

	err = makeRandsHeader(workdir+"/rands.h", 255)
	if err != nil {
		return
	}

	files, err := ioutil.ReadDir(csource + "/hash")
	if err != nil {
		return
	}

	flagBytes, err := generateFlag()
	if err != nil {
		return
	}

	flag = fmt.Sprintf("%x", flagBytes)

	err = makeFlagHeader(workdir+"/flag.h", flagBytes)
	if err != nil {
		return
	}

	file := files[rand.Intn(len(files))]

	stdout, _, err := system("gcc", "-std=c99", "-I"+workdir,
		csource+"/get_hashed_key.c", csource+"/hash/"+file.Name(),
		"-o", workdir+"/get_hash_key")
	if err != nil {
		return
	}

	stdout, _, err = system(workdir + "/get_hash_key")
	if err != nil {
		return
	}

	f, err := os.Create(workdir + "/hashedflag.h")
	if err != nil {
		return
	}
	defer f.Close()

	fmt.Fprint(f, stdout)

	stdout, _, err = system("gcc", "-std=c99", "-I"+workdir,
		csource+"/crackme.c", csource+"/hash/"+file.Name(),
		"-o", path)
	if err != nil {
		return
	}

	_, _, err = system("strip", path)
	if err != nil {
		return
	}

	return
}
