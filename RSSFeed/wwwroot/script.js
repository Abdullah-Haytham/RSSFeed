
const emailRegex = /^[a-zA-Z0-9._%±]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
const noSpacesRegex = /^\S+$/;

document.body.addEventListener('htmx:afterRequest', function (evt) {
    if (evt.detail.requestConfig.path === "/check-page") {
        const emailInput = document.getElementById("email")
        const passwordInput = document.getElementById("password")

        if (emailInput != null && passwordInput != null) {
            const emailVal = document.getElementsByClassName("email-val")[0]
            const passwordVal = document.getElementsByClassName("pass-val")[0]

            emailInput.addEventListener("blur", (e) => { emailValidation(emailInput, emailVal); })
            passwordInput.addEventListener("blur", (e) => {passwordValidation(passwordInput, passwordVal) })
        }
    }

    if (evt.detail.requestConfig.path === "/login") {
        alertBox = document.getElementById("alert-box");
        if (alertBox.innerHTML === "User logged in successfully") {
            alert("Login Successful")
            htmx.ajax("GET", "/check-page", { target: ".replace" })
        } else {
        }
    }

    if (evt.detail.requestConfig.path === "/register") {
        secondDelay()
        alertBox = document.getElementById("alert-box");
        if (alertBox.innerHTML === "User Created successfully") {
            alert("User Created successfully")
            htmx.ajax("GET", "/login-page", { target: ".replace" })
        } else {
            alert("Error! Try Registering Again")
        }
    }

    if (evt.detail.requestConfig.path === "logout") {
        htmx.ajax("GET", "/login-page", { target: ".replace" })
        alert("Logout Successfull")
    }

    if (evt.detail.requestConfig.path === "add-feed") {
        const addMessage = document.getElementById("add-message")

        if (addMessage.innerHTML === "Feed Added Successfully") {
            alert("Feed Added Successfully")
            htmx.ajax("GET", "/feeds", { target: ".feed-container" })
            htmx.ajax("GET", "/shortcuts", { target: ".shortcuts" })
            htmx.ajax("GET", "/select-options", { target: ".form-select" })
        } else {
            alert("Error! Feed not added")
        }
        
    }

    if (evt.detail.requestConfig.path === "delete-feed") {
        const deleteMessage = document.getElementById("delete-message")

        if (deleteMessage.innerHTML === "Feed Deleted Successfully") {
            alert("Feed Deleted Successfully")
            htmx.ajax("GET", "/feeds", { target: ".feed-container" })
            htmx.ajax("GET", "/shortcuts", { target: ".shortcuts" })
            htmx.ajax("GET", "/select-options", { target: ".form-select" })
        } else {
            alert("Error! Feed not Deleted")
        }

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
