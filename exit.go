package main

import (
	"fmt"
	"log"
	"os"
)

func exitWithError(err error) {
	log.Println(err)
	fmt.Println("Press Enter to exit...")
	var temp string
	fmt.Scanln(&temp)
	os.Exit(1)
}
