package main

import (
	"crypto/md5"
	"crypto/sha512"
	"fmt"
	"io/ioutil"
	"net/http"
	"os"

	"github.com/naoina/toml"
)

type team struct {
	Name      string
	Email     string
	About     string
	IPAddress string
}

func authHandler(w http.ResponseWriter, r *http.Request) {

	if r.Method != "POST" {
		http.Redirect(w, r, "/", 307)
		return
	}

	name := r.FormValue("name")
	if name == "" {
		http.Redirect(w, r, "/", 307)
		return
	}

	if len(name) > 40 {
		http.Redirect(w, r, "/invalid_name.html", 307)
		return
	}

	_, err := os.Stat("teams/" + fmt.Sprintf("%x", md5.Sum([]byte(name))))
	if err == nil {
		http.Redirect(w, r, "/already_registered.html", 307)
		return
	}

	fmt.Println("stat", err)

	email := r.FormValue("email")
	if email == "" {
		http.Redirect(w, r, "/", 307)
		return
	}

	about := r.FormValue("about")
	if about == "" {
		http.Redirect(w, r, "/", 307)
		return
	}

	captcha := r.FormValue("captcha")
	if captcha == "" {
		http.Redirect(w, r, "/", 307)
		return
	}

	captchaHash, err := r.Cookie("PHPSESSID")
	if err != nil {
		http.Redirect(w, r, "/", 307)
		return
	}

	if captchaHash.Value != fmt.Sprintf("%x", sha512.Sum512([]byte(captcha))) {
		http.Redirect(w, r, "/invalid_captcha.html", 307)
		return
	}

	ip := r.Header.Get("X-Forwarded-For")
	if ip == "" {
		ip = r.RemoteAddr
	}

	buf, err := toml.Marshal(team{Name: name, Email: email, About: about, IPAddress: ip})
	if err != nil {
		http.Redirect(w, r, "/", 307)
		return
	}

	err = ioutil.WriteFile("teams/"+fmt.Sprintf("%x", md5.Sum([]byte(name))), buf, 0644)
	if err != nil {
		http.Redirect(w, r, "/fail.html", 307)
		return
	}

	http.Redirect(w, r, "/success.html", 307)
}

func getCaptcha() (captcha, path string) {
	captcha = "truecaptcha"
	path = "/bin/true"
	return
}

func captchaHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Cache-control", "private")

	captcha, path := getCaptcha()

	http.SetCookie(w, &http.Cookie{
		Name:  "PHPSESSID",
		Value: fmt.Sprintf("%x", sha512.Sum512([]byte(captcha))),
	})

	http.ServeFile(w, r, path)
}

func main() {

	//http.Handle("/", http.FileServer(http.Dir("/home/mikhail/dev/go/src/github.com/jollheef/ibst-psu-ctf-4-register/www/")))

	http.HandleFunc("/auth.brainfuck", http.HandlerFunc(authHandler))
	http.HandleFunc("/crackme", http.HandlerFunc(captchaHandler))

	err := http.ListenAndServe(":8080", nil)
	if err != nil {
		return
	}
}
