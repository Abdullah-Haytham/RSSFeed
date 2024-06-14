const loginForm = document.getElementById("login-form");
const emailVal = document.getElementsByClassName("email-val")[0]
const passwordVal = document.getElementsByClassName("pass-val")[0]

if(loginForm != null && emailVal != null && passwordVal != null){
loginForm.addEventListener('submit', (e) => {
    e.preventDefault();

    console.log("Login submitted")
    emailVal.innerHTML = "";
    passwordVal.innerHTML = "";

    const email = loginForm.email.value;
    const password = loginForm.password.value;

    email === "" ? emailVal.innerHTML = "Email is required" : emailVal.innerHTML = "";
    password === "" ? passwordVal.innerHTML = "Password is required" : passwordVal.innerHTML = "";

    password.length < 8 && password.length > 0 ? passwordVal.innerHTML = "Password must be at least 8 characters" : passwordVal.innerHTML = "";

    if (emailVal.innerHTML !== "" || passwordVal.innerHTML !== "") {
        return;
    }

    const formData = new FormData();
    formData.append('email', email);
    formData.append('password', password);

    fetch('/login', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok){
                console.log("Invalid credentials")
                alertBox = document.getElementById("alert-box");
                alertBox.classList.add("red")
                alertBox.innerHTML = "Invalid credentials"
            }else{
                console.log("Login Successful")
                alertBox = document.getElementById("alert-box");
                alertBox.classList.add("green")
                alertBox.innerHTML = "Login Successful!"
            }
        })
        .catch(error => {
            // handle error
        });
});
}

document.body.addEventListener('htmx:afterRequest', function (evt) {
    if (evt.detail.requestConfig.path === "/login") {
        console.log("tried login")
        alertBox = document.getElementById("alert-box");
        if (alertBox.classList.contains("green")) {
            console.log("Login Successful")
            HTMLTextAreaElement.ajax("GET", "/check-page", {target: ".replace"})
        }
    }
}
)