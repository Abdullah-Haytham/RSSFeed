
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
            htmxGet("/feeds", ".feed-container")
                .then(() => {
                    return htmxGet("/shortcuts", ".shortcuts");
                })
                .then(() => {
                    return htmxGet("/select-options", ".form-select");
                })
                .then(() => {
                    console.log("All AJAX calls completed successfully");
                })
                .catch(error => {
                    console.error("Error during AJAX calls:", error);
                });
        } else {
            alert(addMessage.innerHTML)
        }
        
    }

    if (evt.detail.requestConfig.path === "delete-feed") {
        const deleteMessage = document.getElementById("delete-message")

        if (deleteMessage.innerHTML === "Feed Deleted Successfully") {
            alert("Feed Deleted Successfully")
            htmxGet("/feeds", ".feed-container")
                .then(() => {
                    return htmxGet("/shortcuts", ".shortcuts");
                })
                .then(() => {
                    return htmxGet("/select-options", ".form-select");
                })
                .then(() => {
                    console.log("All AJAX calls completed successfully");
                })
                .catch(error => {
                    console.error("Error during AJAX calls:", error);
                });
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

function htmxGet(url, target) {
    return new Promise((resolve, reject) => {
        htmx.ajax("GET", url, { target: target })
            .then(response => resolve(response))
            .catch(error => reject(error));
    });
}

function toggleMenu() {
    let menu = document.querySelector('.menu');
    let sidebar = document.querySelector('.sidebar-container');
    let main = document.querySelector('.main-container');
    sidebar.classList.toggle('active');
    main.classList.toggle('no-grid')
}
