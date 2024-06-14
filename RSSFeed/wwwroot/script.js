
const emailRegex = /^[a-zA-Z0-9._%±]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
const noSpacesRegex = /^\S+$/;

document.body.addEventListener('htmx:afterRequest', function (evt) {
    if (evt.detail.requestConfig.path === "/check-page") {
        console.log("check-page wooo")
        const emailInput = document.getElementById("email")
        const passwordInput = document.getElementById("password")

        if (emailInput != null && passwordInput != null) {
            console.log("fi 7aga b null")
            const emailVal = document.getElementsByClassName("email-val")[0]
            const passwordVal = document.getElementsByClassName("pass-val")[0]

            emailInput.addEventListener("blur", (e) => { emailValidation(emailInput, emailVal); })
            passwordInput.addEventListener("blur", (e) => {passwordValidation(passwordInput, passwordVal) })
        }
    }

    if (evt.detail.requestConfig.path === "/login") {
        console.log("tried login")
        alertBox = document.getElementById("alert-box");
        console.log(alertBox.innerHTML)
        if (alertBox.innerHTML === "User logged in successfully") {
            console.log("Login Successful")
            htmx.ajax("GET", "/check-page", { target: ".replace" })
        }
    }

    if (evt.detail.requestConfig.path === "/register") {
        secondDelay()
        console.log("tried register")
        alertBox = document.getElementById("alert-box");
        console.log(alertBox.innerHTML)
        if (alertBox.innerHTML === "User Created successfully") {
            console.log("Register Successful")
            htmx.ajax("GET", "/login-page", { target: ".replace" })
        }
    }

    if (evt.detail.requestConfig.path === "logout") {
        htmx.ajax("GET", "/login-page", { target: ".replace" })
    }
}
)

function emailValidation(emailInput, emailVal) {
    const email = emailInput.value.trim()
    if (email === "") {
        emailVal.innerHTML = "Email is required"
    } else if (!emailRegex.test(email)) {
        emailVal.innerHTML = "Enter a valid Email"
    } else {
        emailVal.innerHTML = ""
    }
}

function passwordValidation(passwordInput, passwordVal) {
    const password = passwordInput.value.trim()
    if (password === "") {
        passwordVal.innerHTML = "Password is required"
    } else if (!noSpacesRegex.test(password)) {
        passwordVal.innerHTML = "Password can't have spaces"
    } else {
        passwordVal.innerHTML = ""
    }
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function secondDelay() {
    await sleep(1000)
}
