// ---------- Shared helpers ----------
function showFieldError(inputEl, errorEl, message) {
    errorEl.textContent = message;
    errorEl.style.display = 'block';
    if (inputEl) inputEl.classList.add('input-invalid');
}

function clearFieldError(inputEl, errorEl) {
    errorEl.textContent = '';
    errorEl.style.display = 'none';
    if (inputEl) inputEl.classList.remove('input-invalid');
}

function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

// Formats raw digits into Malaysian phone format
// 011 + 8 digits -> 011-XXX XXXXX
// 01X (X != 1) + 7 digits -> 01X-XXX XXXX
// other 9 digits -> XX-XXX XXXX
function formatMalaysianPhone(raw) {
    const digits = raw.replace(/\D/g, '');

    if (digits.startsWith('011')) {
        // 011 only accepts 8 digits total (011-XXXXXXXX)
        if (digits.length === 11) {
            return digits.slice(0, 3) + '-' + digits.slice(3);
        }
        return digits; // wrong length, leave as-is, validation will catch it
    }
    if (digits.startsWith('01') && digits.length === 10) {
        return digits.slice(0, 3) + '-' + digits.slice(3);
    }
    if (digits.length === 9) {
        return digits.slice(0, 2) + '-' + digits.slice(2);
    }
    return digits; // Not a recognized length, leave as digits, validation will catch it
}

function isValidMalaysianPhone(formatted) {
    // 011 must be 8 digits; other 01X (X = 0, 2-9) must be 7 digits; other must be 7 digits
    return /^(011-\d{8}|01[02-9]-\d{7}|\d{2}-\d{7})$/.test(formatted);
}

function isStrongPassword(password) {
    // At least 8 chars, 1 uppercase, 1 digit, 1 special character
    return /^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_\-+=\[\]{};:'",.<>\/?\\|`~]).{8,}$/.test(password);
}

// Returns 'weak' | 'medium' | 'strong' | null (null = empty)
function getPasswordStrength(password) {
    if (!password) return null;

    let score = 0;
    if (password.length >= 8) score++;
    if (password.length >= 12) score++;
    if (/[A-Z]/.test(password)) score++;
    if (/[a-z]/.test(password)) score++;
    if (/\d/.test(password)) score++;
    if (/[!@#$%^&*()_\-+=\[\]{};:'",.<>\/?\\|`~]/.test(password)) score++;

    if (score <= 2) return 'weak';
    if (score <= 4) return 'medium';
    return 'strong';
}

function updatePasswordStrengthUI(strengthWrapper, strength) {
    if (!strengthWrapper) return;

    strengthWrapper.classList.remove('strength-weak', 'strength-medium', 'strength-strong');
    const label = strengthWrapper.querySelector('.strength-label');

    if (!strength) {
        strengthWrapper.style.display = 'none';
        return;
    }

    strengthWrapper.style.display = 'block';
    strengthWrapper.classList.add('strength-' + strength);

    if (strength === 'weak') label.textContent = 'Weak password';
    else if (strength === 'medium') label.textContent = 'Medium strength';
    else label.textContent = 'Strong password';
}

// ---------- Login Page ----------
function initLoginForm(returnUrl) {
    const form = document.getElementById('loginForm');
    if (!form) return;

    const loginBtn = document.getElementById('loginBtn');
    const summaryError = document.getElementById('summaryError');
    const emailInput = document.getElementById('Email');
    const passwordInput = document.getElementById('Password');
    const emailError = document.getElementById('emailError');
    const passwordError = document.getElementById('passwordError');

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        summaryError.style.display = 'none';
        clearFieldError(emailInput, emailError);
        clearFieldError(passwordInput, passwordError);

        const email = emailInput.value.trim();
        const password = passwordInput.value;

        let valid = true;
        if (!email) { showFieldError(emailInput, emailError, 'Please enter your email'); valid = false; }
        else if (!isValidEmail(email)) { showFieldError(emailInput, emailError, 'Invalid email format'); valid = false; }
        if (!password) { showFieldError(passwordInput, passwordError, 'Please enter your password'); valid = false; }
        if (!valid) return;

        loginBtn.disabled = true;
        loginBtn.textContent = 'Logging in...';

        try {
            const response = await fetch('/Account/Login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ Email: email, Password: password, ReturnUrl: returnUrl })
            });
            const result = await response.json();

            if (result.success) {
                window.location.href = result.redirectUrl;
            } else {
                summaryError.textContent = result.errors.join(' ');
                summaryError.style.display = 'block';
            }
        } catch (err) {
            summaryError.textContent = 'Something went wrong. Please try again.';
            summaryError.style.display = 'block';
        } finally {
            loginBtn.disabled = false;
            loginBtn.textContent = 'Sign In';
        }
    });
}

// ---------- Register Page (used by both Register.cshtml and AdminRegister.cshtml) ----------
// endpoint: URL to POST to
// includeRole: true if this form has the Member/Trainer role toggle (Register.cshtml)
// includePhone: true if this form collects a phone number (Register.cshtml, false for AdminRegister.cshtml)
function initRegisterForm(endpoint, includeRole, includePhone) {
    if (includePhone === undefined) includePhone = true;

    const form = document.getElementById('registerForm');
    if (!form) return;

    const registerBtn = document.getElementById('registerBtn');
    const summaryError = document.getElementById('summaryError');
    const summarySuccess = document.getElementById('summarySuccess');

    const fullNameInput = document.getElementById('FullName');
    const emailInput = document.getElementById('Email');
    const phoneInput = document.getElementById('PhoneNumber');
    const passwordInput = document.getElementById('Password');
    const confirmInput = document.getElementById('ConfirmPassword');

    const fullNameError = document.getElementById('FullNameError');
    const emailError = document.getElementById('EmailError');
    const phoneError = document.getElementById('PhoneNumberError');
    const passwordError = document.getElementById('PasswordError');
    const confirmError = document.getElementById('ConfirmPasswordError');

    const strengthWrapper = document.getElementById('passwordStrength');

    if (passwordInput && strengthWrapper) {
        passwordInput.addEventListener('input', function () {
            const strength = getPasswordStrength(passwordInput.value);
            updatePasswordStrengthUI(strengthWrapper, strength);
        });
    }

    if (phoneInput) {
        phoneInput.addEventListener('blur', function () {
            if (phoneInput.value.trim() === '') return;
            phoneInput.value = formatMalaysianPhone(phoneInput.value);
        });
    }

    const roleButtons = document.querySelectorAll('.role-btn[data-role]');
    const roleInput = document.getElementById('Role');

    roleButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            roleButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            if (roleInput) roleInput.value = btn.getAttribute('data-role');
        });
    });

    const genderButtons = document.querySelectorAll('.role-btn[data-gender]');
    const genderInput = document.getElementById('Gender');

    genderButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            genderButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            if (genderInput) genderInput.value = btn.getAttribute('data-gender');
        });
    });

    // ↓↓↓ 这段现在正确地留在函数里面了
    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        summaryError.style.display = 'none';
        if (summarySuccess) summarySuccess.style.display = 'none';

        clearFieldError(fullNameInput, fullNameError);
        clearFieldError(emailInput, emailError);
        if (includePhone) clearFieldError(phoneInput, phoneError);
        clearFieldError(passwordInput, passwordError);
        clearFieldError(confirmInput, confirmError);

        if (includePhone && phoneInput) {
            phoneInput.value = formatMalaysianPhone(phoneInput.value);
        }

        const data = {
            FullName: fullNameInput.value.trim(),
            Email: emailInput.value.trim(),
            PhoneNumber: includePhone && phoneInput ? phoneInput.value.trim() : null,
            Gender: genderInput ? genderInput.value : 'Male',
            Password: passwordInput.value,
            ConfirmPassword: confirmInput.value
        };

        if (includeRole) {
            data.Role = roleInput ? roleInput.value : 'Member';
        } else {
            data.Role = 'Admin';
        }

        let valid = true;

        if (!data.FullName) { showFieldError(fullNameInput, fullNameError, 'Please enter your full name'); valid = false; }

        if (!data.Email) { showFieldError(emailInput, emailError, 'Please enter your email'); valid = false; }
        else if (!isValidEmail(data.Email)) { showFieldError(emailInput, emailError, 'Invalid email format'); valid = false; }

        if (includePhone) {
            if (!data.PhoneNumber) { showFieldError(phoneInput, phoneError, 'Please enter your phone number'); valid = false; }
            else if (!isValidMalaysianPhone(data.PhoneNumber)) { showFieldError(phoneInput, phoneError, 'Invalid Malaysian phone number format'); valid = false; }
        }

        if (!data.Password) { showFieldError(passwordInput, passwordError, 'Please enter a password'); valid = false; }
        else if (!isStrongPassword(data.Password)) { showFieldError(passwordInput, passwordError, 'Password must be at least 8 characters with an uppercase letter, a number, and a special character'); valid = false; }

        if (data.ConfirmPassword !== data.Password) { showFieldError(confirmInput, confirmError, 'Passwords do not match'); valid = false; }

        if (!valid) return;

        registerBtn.disabled = true;
        registerBtn.textContent = 'Registering...';

        try {
            const response = await fetch(endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });
            const result = await response.json();

            if (result.success) {
                if (summarySuccess) {
                    summarySuccess.textContent = 'Registration successful! Redirecting to login...';
                    summarySuccess.style.display = 'block';
                }
                setTimeout(() => { window.location.href = '/Account/Login'; }, 1500);
            } else {
                summaryError.textContent = result.errors.join(' ');
                summaryError.style.display = 'block';
            }
        } catch (err) {
            summaryError.textContent = 'Something went wrong. Please try again.';
            summaryError.style.display = 'block';
        } finally {
            registerBtn.disabled = false;
            registerBtn.textContent = includeRole ? 'Register' : 'Create Admin';
        }
    });
}