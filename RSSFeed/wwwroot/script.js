const loginForm = document.getElementById("login-form");
const emailVal = document.getElementsByClassName("email-val")[0]
const passwordVal = document.getElementsByClassName("pass-val")[0]
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
});