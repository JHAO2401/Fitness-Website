let modalUserId = null;

function openEditModalFromRow(btn) {
    const row = btn.closest('tr');
    openEditModal(row);
}

function openEditModal(row) {
    modalUserId = parseInt(row.dataset.userId);

    document.getElementById('modalUserName').textContent = 'Edit: ' + row.dataset.fullName;

    const modalError = document.getElementById('modalError');
    if (modalError) modalError.style.display = 'none';

    const avatarEl = document.getElementById('modalAvatar');
    if (avatarEl) {
        avatarEl.src = row.dataset.avatar && row.dataset.avatar !== '' ? row.dataset.avatar : '/images/logo.png';
    }
    const avatarNameEl = document.getElementById('modalAvatarName');
    if (avatarNameEl) avatarNameEl.textContent = row.dataset.fullName;

    document.getElementById('modalEmail').textContent = row.dataset.email;
    document.getElementById('modalPhone').textContent = row.dataset.phone;
    document.getElementById('modalRole').textContent = row.dataset.role;
    document.getElementById('modalStatus').textContent = row.dataset.isActive === 'true' ? 'Active' : 'Inactive';
    document.getElementById('modalFailedAttempts').textContent = row.dataset.failedAttempts;
    document.getElementById('modalJoined').textContent = row.dataset.joined;

    const isLocked = row.dataset.isLocked === 'true';
    const lockStatusEl = document.getElementById('modalLockStatus');
    const unlockBtn = document.getElementById('modalUnlockBtn');

    if (isLocked) {
        lockStatusEl.innerHTML = '<span class="lock-warning">⚠️ This account is locked due to 3 failed login attempts.</span>';
        unlockBtn.style.display = 'block';
    } else {
        lockStatusEl.innerHTML = '<span class="ok-text">No lock issues. Account security is normal.</span>';
        unlockBtn.style.display = 'none';
    }

    document.getElementById('editModalOverlay').classList.add('open');
}

function closeEditModal() {
    document.getElementById('editModalOverlay').classList.remove('open');
    modalUserId = null;
}

async function unlockUserModal() {
    const modalError = document.getElementById('modalError');
    modalError.style.display = 'none';

    try {
        const response = await fetch('/Admin/UnlockUser', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ userId: modalUserId })
        });
        const result = await response.json();

        if (result.success) {
            location.reload();
        } else {
            modalError.textContent = result.message || 'Failed to unlock user';
            modalError.style.display = 'block';
        }
    } catch (err) {
        modalError.textContent = 'Something went wrong. Please try again.';
        modalError.style.display = 'block';
    }
}

document.addEventListener('DOMContentLoaded', function () {
    const overlay = document.getElementById('editModalOverlay');
    if (overlay) {
        overlay.addEventListener('click', function (e) {
            if (e.target === this) closeEditModal();
        });
    }
});