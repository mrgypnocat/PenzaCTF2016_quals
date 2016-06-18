package main

import (
	"crypto/md5"
	"crypto/sha512"
	"database/sql"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"os"

	"github.com/naoina/toml"

	_ "github.com/mattn/go-sqlite3"
)

var db *sql.DB

type team struct {
	Name      string
	Email     string
	About     string
	IPAddress string
}

func createCaptchaTable() (err error) {

	_, err = db.Exec(`
        CREATE TABLE IF NOT EXISTS "captcha" (
                id         INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
                captcha    TEXT NOT NULL UNIQUE,
                used       BOOLEAN DEFAULT FALSE
        )`)

	return
}

func addCaptcha(captcha string) error {

	stmt, err := db.Prepare("INSERT INTO `captcha` " +
		"(`captcha`) VALUES (?)")
	if err != nil {
		return err
	}

	defer stmt.Close()

	_, err = stmt.Exec(captcha)
	if err != nil {
		return err
	}

	return nil

}

func useCaptcha(captcha string) error {

	stmt, err := db.Prepare("UPDATE `captcha` " +
		"SET used=? where captcha=?")
	if err != nil {
		return err
	}

	defer stmt.Close()

	_, err = stmt.Exec(true, captcha)
	if err != nil {
		return err
	}

	return nil
}

func isCaptchaExists(captcha string) (exists bool) {

	stmt, err := db.Prepare(
		"SELECT EXISTS(SELECT `id` FROM `captcha` WHERE `captcha`=?)")
	if err != nil {
		return false
	}

	defer stmt.Close()

	err = stmt.QueryRow(captcha).Scan(&exists)
	if err != nil {
		return false
	}

	return
}

func isCaptchaUsed(captcha string) (exists bool) {

	stmt, err := db.Prepare(
		"SELECT EXISTS(SELECT `id` FROM `captcha` WHERE `captcha`=? AND `used`=?)")
	if err != nil {
		return false
	}

	defer stmt.Close()

	err = stmt.QueryRow(captcha, true).Scan(&exists)
	if err != nil {
		return false
	}

	return
}

func createSchema() error {

	err := createCaptchaTable()
	if err != nil {
		return err
	}

	return nil
}

func openDatabase(path string) (err error) {

	db, err = sql.Open("sqlite3", path)
	if err != nil {
		return
	}

	err = createSchema()
	if err != nil {
		return
	}

	return
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

	if isCaptchaUsed(captcha) || !isCaptchaExists(captcha) {
		http.Redirect(w, r, "/invalid_captcha.html", 307)
		return
	}

	err = useCaptcha(captcha)
	if err != nil {
		http.Redirect(w, r, "/", 307)
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

func captchaHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Cache-control", "private")

	workdir, err := ioutil.TempDir("/tmp/", "revcaptcha_")
	if err != nil {
		log.Println("Error:", err)
		return
	}

	//captcha, path, err := makeCaptcha(workdir, "/home/mikhail/dev/ibst.psu-ctf-iv-quals/common/signup/csource/")
	captcha, path, err := makeCaptcha(workdir, "/home/website/csource/")
	if err != nil {
		log.Println("Error:", err)
		return
	}

	http.SetCookie(w, &http.Cookie{
		Name:  "PHPSESSID",
		Value: fmt.Sprintf("%x", sha512.Sum512([]byte(captcha))),
	})

	http.ServeFile(w, r, path)

	err = os.RemoveAll(workdir)
	if err != nil {
		log.Println("Error:", err)
		return
	}

	ip := r.Header.Get("X-Forwarded-For")
	if ip == "" {
		ip = r.RemoteAddr
	}

	err = addCaptcha(captcha)
	if err != nil {
		log.Println("Error:", err)
		return
	}

	log.Printf("Send captcha '%s' to %s", captcha, ip)
}

func main() {

	openDatabase("/tmp/sqlite.db")

	//http.Handle("/", http.FileServer(http.Dir("/home/mikhail/dev/ibst.psu-ctf-iv-quals/common/signup/www")))

	http.HandleFunc("/auth.brainfuck", http.HandlerFunc(authHandler))
	http.HandleFunc("/crackme", http.HandlerFunc(captchaHandler))

	err := http.ListenAndServe(":8080", nil)
	if err != nil {
		log.Println("Error:", err)
	}
}
