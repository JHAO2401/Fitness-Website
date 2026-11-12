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

function initProfilePage() {
    const form = document.getElementById('profileForm');
    if (!form) return;

    const saveBtn = document.getElementById('saveProfileBtn');
    const summaryError = document.getElementById('summaryError');
    const summarySuccess = document.getElementById('summarySuccess');

    const fullNameInput = document.getElementById('FullName');
    const emailInput = document.getElementById('Email');
    const currentPwInput = document.getElementById('CurrentPassword');
    const newPwInput = document.getElementById('NewPassword');
    const confirmPwInput = document.getElementById('ConfirmNewPassword');

    const genderButtons = document.querySelectorAll('.gender-btn');
    const genderInput = document.getElementById('Gender');

    genderButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            genderButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            if (genderInput) genderInput.value = btn.getAttribute('data-gender');
        });
    });

    const strengthWrapper = document.getElementById('passwordStrength');
    if (newPwInput && strengthWrapper) {
        newPwInput.addEventListener('input', function () {
            const strength = getPasswordStrength(newPwInput.value);
            updatePasswordStrengthUI(strengthWrapper, strength);
        });

    }

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        summaryError.style.display = 'none';
        summarySuccess.style.display = 'none';

        const payload = {
            UserId: parseInt(document.getElementById('UserId').value),
            FullName: fullNameInput.value.trim(),
            Email: emailInput.value.trim(),
            Gender: genderInput ? genderInput.value : null,
            CurrentPassword: currentPwInput.value || null,
            NewPassword: newPwInput.value || null,
            ConfirmNewPassword: confirmPwInput.value || null
        };

        if (payload.NewPassword && payload.NewPassword === payload.CurrentPassword) {
            summaryError.textContent = 'New password must be different from the current password.';
            summaryError.style.display = 'block';
            return;
        }

        saveBtn.disabled = true;
        saveBtn.textContent = 'Saving...';

        try {
            const response = await fetch('/Profile/UpdateProfile', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const result = await response.json();

            if (result.success) {
                summarySuccess.textContent = 'Profile updated successfully.';
                summarySuccess.style.display = 'block';
                currentPwInput.value = '';
                newPwInput.value = '';
                confirmPwInput.value = '';
                updatePasswordStrengthUI(strengthWrapper, null);
            } else {
                summaryError.textContent = result.errors.join(' ');
                summaryError.style.display = 'block';
            }
        } catch (err) {
            summaryError.textContent = 'Something went wrong. Please try again.';
            summaryError.style.display = 'block';
        } finally {
            saveBtn.disabled = false;
            saveBtn.textContent = 'Save Changes';
        }
    });

    const avatarInput = document.getElementById('avatarInput');
    const avatarPreview = document.getElementById('avatarPreview');
    const avatarError = document.getElementById('avatarError');

    if (avatarInput) {
        avatarInput.addEventListener('change', async function () {
            const file = avatarInput.files[0];
            if (!file) return;

            avatarError.style.display = 'none';

            const formData = new FormData();
            formData.append('avatar', file);

            try {
                const response = await fetch('/Profile/UploadAvatar', {
                    method: 'POST',
                    body: formData
                });

                if (!response.ok) {
                    console.error('UploadAvatar HTTP status:', response.status);
                    avatarError.textContent = `Upload failed (HTTP ${response.status}).`;
                    avatarError.style.display = 'block';
                    return;
                }

                const result = await response.json();

                if (result.success) {
                    avatarPreview.src = result.avatarUrl + '?t=' + Date.now();
                } else {
                    avatarError.textContent = result.message;
                    avatarError.style.display = 'block';
                }
            } catch (err) {
                console.error('UploadAvatar exception:', err);
                avatarError.textContent = 'Upload failed. Please try again.';
                avatarError.style.display = 'block';
            }
        });
    }
}