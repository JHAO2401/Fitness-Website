// ---------- Gym Details Form ----------
const gymInfoForm = document.getElementById('gymInfoForm');
if (gymInfoForm) {
    gymInfoForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        const summaryError = document.getElementById('summaryError');
        const summarySuccess = document.getElementById('summarySuccess');
        summaryError.style.display = 'none';
        summarySuccess.style.display = 'none';

        if (hourRange.from > hourRange.to) {
            summaryError.textContent = 'Start Day must come before or be the same as End Day.';
            summaryError.style.display = 'block';
            return;
        }

        if (!hourRange.isClosed && (!hourRange.openTime || !hourRange.closeTime)) {
            summaryError.textContent = 'Please set both Start Time and End Time, or mark the range as Closed.';
            summaryError.style.display = 'block';
            return;
        }

        if (!hourRange.isClosed && hourRange.closeTime <= hourRange.openTime) {
            summaryError.textContent = 'End Time must be after Start Time.';
            summaryError.style.display = 'block';
            return;
        }

        const payload = {
            GymName: document.getElementById('GymName').value.trim(),
            Address: document.getElementById('Address').value.trim(),
            PhoneNumber: document.getElementById('PhoneNumber').value.trim(),
            Description: document.getElementById('Description').value.trim()
        };

        // Days inside the selected Start Day-End Day span use this range's hours;
        // any day outside the span is treated as Closed since only one range is editable.
        const days = DAY_NAMES.map((name, d) => {
            const inRange = d >= hourRange.from && d <= hourRange.to;
            return {
                DayOfWeek: d,
                DayName: name,
                IsClosed: inRange ? hourRange.isClosed : true,
                OpenTime: inRange ? hourRange.openTime : '00:00',
                CloseTime: inRange ? hourRange.closeTime : '00:00'
            };
        });



        const btn = document.getElementById('saveGymInfoBtn');
        btn.disabled = true;
        btn.textContent = 'Saving...';

        try {
            const [infoResponse, hoursResponse] = await Promise.all([
                fetch('/Admin/UpdateGymInfo', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                }),
                fetch('/Admin/UpdateOperatingHours', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ days })
                })
            ]);

            const infoResult = await infoResponse.json();
            const hoursResult = await hoursResponse.json();

            const errors = [];
            if (!infoResult.success) errors.push(...infoResult.errors);
            if (!hoursResult.success) errors.push(...hoursResult.errors);

            if (errors.length === 0) {
                summarySuccess.textContent = 'Gym information and operating hours updated successfully.';
                summarySuccess.style.display = 'block';
            } else {
                summaryError.textContent = errors.join(' ');
                summaryError.style.display = 'block';
            }
        } catch (err) {
            summaryError.textContent = 'Something went wrong. Please try again.';
            summaryError.style.display = 'block';
        } finally {
            btn.disabled = false;
            btn.textContent = 'Save Changes';
        }
    });
}

// ---------- Photo Gallery ----------
const photoInput = document.getElementById('photoInput');
const addPhotoBtn = document.getElementById('addPhotoBtn');
const photoDropzone = document.getElementById('photoDropzone');

if (addPhotoBtn && photoInput) {
    addPhotoBtn.addEventListener('click', function () {
        photoInput.click();
    });
}

if (photoInput) {
    photoInput.addEventListener('change', function () {
        const file = photoInput.files[0];
        if (file) uploadGymPhoto(file);
        photoInput.value = '';
    });
}

if (photoDropzone) {
    ['dragenter', 'dragover'].forEach(evt => {
        photoDropzone.addEventListener(evt, function (e) {
            e.preventDefault();
            e.stopPropagation();
            photoDropzone.classList.add('drag-active');
        });
    });

    ['dragleave', 'drop'].forEach(evt => {
        photoDropzone.addEventListener(evt, function (e) {
            e.preventDefault();
            e.stopPropagation();
            photoDropzone.classList.remove('drag-active');
        });
    });

    photoDropzone.addEventListener('drop', function (e) {
        const files = e.dataTransfer.files;
        if (files && files.length > 0) {
            uploadGymPhoto(files[0]);
        }
    });
}

async function uploadGymPhoto(file) {
    const photoError = document.getElementById('photoError');
    photoError.style.display = 'none';

    if (!file.type.startsWith('image/')) {
        photoError.textContent = 'Only image files are allowed.';
        photoError.style.display = 'block';
        return;
    }

    const formData = new FormData();
    formData.append('photo', file);

    try {
        const response = await fetch('/Admin/UploadGymPhoto', {
            method: 'POST',
            body: formData
        });
        const result = await response.json();

        if (result.success) {
            location.reload();
        } else {
            photoError.textContent = result.message;
            photoError.style.display = 'block';
        }
    } catch (err) {
        photoError.textContent = 'Upload failed. Please try again.';
        photoError.style.display = 'block';
    }
}

async function setCoverPhoto(photoId) {
    try {
        const response = await fetch('/Admin/SetCoverPhoto', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ gymPhotoId: photoId })
        });
        const result = await response.json();

        if (result.success) {
            location.reload();
        } else {
            alert(result.message || 'Failed to set cover photo');
        }
    } catch (err) {
        alert('Something went wrong. Please try again.');
    }
}

let photoToDelete = null;

function confirmDeletePhoto(photoId) {
    photoToDelete = photoId;
    document.getElementById('deletePhotoError').style.display = 'none';
    document.getElementById('deletePhotoModalOverlay').classList.add('open');
}

function closeDeletePhotoModal() {
    document.getElementById('deletePhotoModalOverlay').classList.remove('open');
    photoToDelete = null;
}

async function deletePhoto() {
    if (!photoToDelete) return;

    const errorEl = document.getElementById('deletePhotoError');
    const btn = document.getElementById('confirmDeletePhotoBtn');
    errorEl.style.display = 'none';
    btn.disabled = true;
    btn.textContent = 'Deleting...';

    try {
        const response = await fetch('/Admin/DeleteGymPhoto', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ gymPhotoId: photoToDelete })
        });
        const result = await response.json();

        if (result.success) {
            location.reload();
        } else {
            errorEl.textContent = result.message || 'Failed to delete photo';
            errorEl.style.display = 'block';
        }
    } catch (err) {
        errorEl.textContent = 'Something went wrong. Please try again.';
        errorEl.style.display = 'block';
    } finally {
        btn.disabled = false;
        btn.textContent = 'Delete';
    }
}

// ---------- Equipment Availability Toggle ----------
function setAvailability(isAvailable) {
    document.getElementById('EquipmentAvailable').value = isAvailable ? 'true' : 'false';

    const btnAvailable = document.getElementById('btnAvailable');
    const btnUnavailable = document.getElementById('btnUnavailable');

    if (isAvailable) {
        btnAvailable.classList.add('active');
        btnUnavailable.classList.remove('active');
    } else {
        btnUnavailable.classList.add('active');
        btnAvailable.classList.remove('active');
    }
}

// ---------- Equipment Modal ----------
let editingEquipmentId = 0;
let selectedEquipmentImageFile = null;

function openAddEquipmentModal() {
    editingEquipmentId = 0;
    selectedEquipmentImageFile = null;
    document.getElementById('equipmentModalTitle').textContent = 'Add Equipment';
    document.getElementById('EquipmentId').value = 0;
    document.getElementById('EquipmentName').value = '';
    document.getElementById('EquipmentDescription').value = '';
    document.getElementById('EquipmentQuantity').value = 1;
    document.getElementById('equipmentImgPreview').src = '/image/logo.png';
    document.getElementById('equipmentImageInput').value = '';
    setAvailability(true);
    document.getElementById('equipmentModalError').style.display = 'none';
    document.getElementById('equipmentModalOverlay').classList.add('open');
}

function openEditEquipmentModal(id, name, description, quantity, isAvailable, imageUrl) {
    editingEquipmentId = id;
    selectedEquipmentImageFile = null;
    document.getElementById('equipmentModalTitle').textContent = 'Edit Equipment';
    document.getElementById('EquipmentId').value = id;
    document.getElementById('EquipmentName').value = name;
    document.getElementById('EquipmentDescription').value = description;
    document.getElementById('EquipmentQuantity').value = quantity;
    document.getElementById('equipmentImgPreview').src = (imageUrl && imageUrl !== '') ? imageUrl : '/image/logo.png';
    document.getElementById('equipmentImageInput').value = '';
    setAvailability(isAvailable);
    document.getElementById('equipmentModalError').style.display = 'none';
    document.getElementById('equipmentModalOverlay').classList.add('open');
}

function closeEquipmentModal() {
    document.getElementById('equipmentModalOverlay').classList.remove('open');
}

// Just preview locally; the actual upload happens after the equipment is saved
function previewEquipmentImage(file) {
    if (!file || !file.type.startsWith('image/')) return;

    selectedEquipmentImageFile = file;

    const reader = new FileReader();
    reader.onload = function (e) {
        document.getElementById('equipmentImgPreview').src = e.target.result;
    };
    reader.readAsDataURL(file);
}

const equipmentImageInput = document.getElementById('equipmentImageInput');
if (equipmentImageInput) {
    equipmentImageInput.addEventListener('change', function () {
        const file = equipmentImageInput.files[0];
        if (file) previewEquipmentImage(file);
    });
}

const equipmentPhotoDropzone = document.getElementById('equipmentPhotoDropzone');
if (equipmentPhotoDropzone) {
    ['dragenter', 'dragover'].forEach(evt => {
        equipmentPhotoDropzone.addEventListener(evt, function (e) {
            e.preventDefault();
            e.stopPropagation();
            equipmentPhotoDropzone.style.borderColor = '#4a4ae0';
            equipmentPhotoDropzone.style.background = '#eef0ff';
        });
    });

    ['dragleave', 'drop'].forEach(evt => {
        equipmentPhotoDropzone.addEventListener(evt, function (e) {
            e.preventDefault();
            e.stopPropagation();
            equipmentPhotoDropzone.style.borderColor = '#ddd';
            equipmentPhotoDropzone.style.background = '#fafafd';
        });
    });

    equipmentPhotoDropzone.addEventListener('drop', function (e) {
        const files = e.dataTransfer.files;
        if (files && files.length > 0) {
            previewEquipmentImage(files[0]);
        }
    });
}
async function uploadSelectedEquipmentImage(equipmentId) {
    if (!selectedEquipmentImageFile) return;

    const formData = new FormData();
    formData.append('image', selectedEquipmentImageFile);

    try {
        await fetch('/Admin/UploadEquipmentImage?equipmentId=' + equipmentId, {
            method: 'POST',
            body: formData
        });
    } catch (err) {
        // Equipment itself was saved successfully; image upload failure is non-fatal here
    }
}
// ---------- Operating Hours: Add-Range Form + List ----------
const DAY_NAMES = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
let hourRange = { from: 0, to: 6, isClosed: false, openTime: '06:00', closeTime: '23:00' };

function dayOptionsHtml(selectedIndex) {
    return DAY_NAMES.map((name, idx) =>
        `<option value="${idx}"${idx === selectedIndex ? ' selected' : ''}>${name}</option>`
    ).join('');
}

function formatTimeLabel(t) {
    if (!t) return '';
    const [h, m] = t.split(':').map(Number);
    const period = h >= 12 ? 'PM' : 'AM';
    const h12 = h % 12 === 0 ? 12 : h % 12;
    return `${h12}:${String(m).padStart(2, '0')} ${period}`;
}

function renderHourRangePreview() {
    const list = document.getElementById('hoursRangeList');
    if (!list) return;
    list.innerHTML = '';

    const item = document.createElement('div');
    item.className = 'hours-range-item' + (hourRange.isClosed ? ' is-closed' : '');
    const dayLabel = hourRange.from === hourRange.to
        ? DAY_NAMES[hourRange.from]
        : `${DAY_NAMES[hourRange.from]} - ${DAY_NAMES[hourRange.to]}`;
    const statusHtml = hourRange.isClosed
        ? `<span class="status-closed">Closed</span>`
        : `<span class="status-open">${formatTimeLabel(hourRange.openTime)} - ${formatTimeLabel(hourRange.closeTime)}</span>`;

    item.innerHTML = `<span class="hours-range-item-text">${dayLabel}: ${statusHtml}</span>`;
    list.appendChild(item);
}

// Reads the current form fields, updates hourRange, validates, and refreshes the live preview.
// Runs immediately on every edit - no "Add" click needed.
function updateHourRange() {
    const errorEl = document.getElementById('hoursError');
    errorEl.style.display = 'none';

    hourRange.from = parseInt(document.getElementById('rangeStartDay').value);
    hourRange.to = parseInt(document.getElementById('rangeEndDay').value);
    hourRange.openTime = document.getElementById('rangeStartTime').value;
    hourRange.closeTime = document.getElementById('rangeEndTime').value;

    if (hourRange.from > hourRange.to) {
        errorEl.textContent = `Start Day must come before or be the same as End Day (currently ${DAY_NAMES[hourRange.from]} to ${DAY_NAMES[hourRange.to]}).`;
        errorEl.style.display = 'block';
    } else if (!hourRange.isClosed && (!hourRange.openTime || !hourRange.closeTime)) {
        errorEl.textContent = 'Please set both Start Time and End Time, or mark this range as Closed.';
        errorEl.style.display = 'block';
    } else if (!hourRange.isClosed && hourRange.closeTime <= hourRange.openTime) {
        errorEl.textContent = 'End Time must be after Start Time.';
        errorEl.style.display = 'block';
    }

    renderHourRangePreview();
}

function toggleRangeClosed() {
    const btn = document.getElementById('rangeClosedToggle');
    const isNowClosed = !btn.classList.contains('active');
    btn.classList.toggle('active', isNowClosed);
    btn.textContent = isNowClosed ? 'Closed' : 'Open';
    document.getElementById('rangeStartTime').disabled = isNowClosed;
    document.getElementById('rangeEndTime').disabled = isNowClosed;
    hourRange.isClosed = isNowClosed;
    updateHourRange();
}

// Used only once, on page load, to derive the starting range from previously saved data.
function groupOperatingHours(rawDays) {
    const sorted = [...rawDays].sort((a, b) => a.DayOfWeek - b.DayOfWeek);
    const groups = [];
    let current = null;

    sorted.forEach(d => {
        const sameAsCurrent = current &&
            current.isClosed === d.IsClosed &&
            current.openTime === d.OpenTime &&
            current.closeTime === d.CloseTime &&
            current.to === d.DayOfWeek - 1;

        if (sameAsCurrent) {
            current.to = d.DayOfWeek;
        } else {
            current = { from: d.DayOfWeek, to: d.DayOfWeek, isClosed: d.IsClosed, openTime: d.OpenTime, closeTime: d.CloseTime };
            groups.push(current);
        }
    });

    return groups;
}

(function initOperatingHours() {
    const dataEl = document.getElementById('operatingHoursData');
    const startDaySelect = document.getElementById('rangeStartDay');
    const endDaySelect = document.getElementById('rangeEndDay');
    const startTimeInput = document.getElementById('rangeStartTime');
    const endTimeInput = document.getElementById('rangeEndTime');
    if (!dataEl || !startDaySelect || !endDaySelect) return;

    let rawDays = [];
    try {
        rawDays = JSON.parse(dataEl.textContent || '[]');
    } catch (err) {
        rawDays = [];
    }

    if (rawDays.length > 0) {
        const groups = groupOperatingHours(rawDays);
        // Only a single range is editable now - start from the widest saved block.
        const widest = groups.reduce((a, b) => (b.to - b.from) > (a.to - a.from) ? b : a, groups[0]);
        hourRange = { from: widest.from, to: widest.to, isClosed: !!widest.isClosed, openTime: widest.openTime, closeTime: widest.closeTime };
    }

    startDaySelect.innerHTML = dayOptionsHtml(hourRange.from);
    endDaySelect.innerHTML = dayOptionsHtml(hourRange.to);
    startTimeInput.value = hourRange.openTime || '06:00';
    endTimeInput.value = hourRange.closeTime || '23:00';
    startTimeInput.disabled = hourRange.isClosed;
    endTimeInput.disabled = hourRange.isClosed;

    const toggleBtn = document.getElementById('rangeClosedToggle');
    if (toggleBtn) {
        toggleBtn.classList.toggle('active', hourRange.isClosed);
        toggleBtn.textContent = hourRange.isClosed ? 'Closed' : 'Open';
    }

    // Live-update the moment the admin edits any field - no "Add" click required.
    startDaySelect.addEventListener('change', updateHourRange);
    endDaySelect.addEventListener('change', updateHourRange);
    startTimeInput.addEventListener('input', updateHourRange);
    endTimeInput.addEventListener('input', updateHourRange);

    renderHourRangePreview();
})();

const equipmentForm = document.getElementById('equipmentForm');
if (equipmentForm) {
    equipmentForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        const errorEl = document.getElementById('equipmentModalError');
        errorEl.style.display = 'none';

        const quantityValue = parseInt(document.getElementById('EquipmentQuantity').value);

        const payload = {
            EquipmentId: editingEquipmentId,
            Name: document.getElementById('EquipmentName').value.trim(),
            Description: document.getElementById('EquipmentDescription').value.trim(),
            Quantity: isNaN(quantityValue) ? 0 : quantityValue,
            IsAvailable: document.getElementById('EquipmentAvailable').value === 'true'
        };

        if (!payload.Name) {
            errorEl.textContent = 'Equipment name is required';
            errorEl.style.display = 'block';
            return;
        }

        if (payload.Quantity < 0) {
            errorEl.textContent = 'Quantity cannot be negative';
            errorEl.style.display = 'block';
            return;
        }

        const isNew = editingEquipmentId === 0;
        const endpoint = isNew ? '/Admin/AddEquipment' : '/Admin/EditEquipment';
        const btn = document.getElementById('saveEquipmentBtn');
        btn.disabled = true;
        btn.textContent = 'Saving...';

        try {
            const response = await fetch(endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const result = await response.json();

            if (result.success) {
                // If a photo was chosen, upload it now that we have a valid EquipmentId
                if (selectedEquipmentImageFile) {
                    const newId = isNew ? result.equipment.equipmentId : editingEquipmentId;
                    await uploadSelectedEquipmentImage(newId);
                }
                location.reload();
            } else {
                errorEl.textContent = result.errors.join(' ');
                errorEl.style.display = 'block';
            }
        } catch (err) {
            errorEl.textContent = 'Something went wrong. Please try again.';
            errorEl.style.display = 'block';
        } finally {
            btn.disabled = false;
            btn.textContent = 'Save Equipment';
        }
    });
}

// ---------- Delete Equipment ----------
let equipmentToDelete = null;

function confirmDeleteEquipment(id) {
    equipmentToDelete = id;
    document.getElementById('deleteEquipmentError').style.display = 'none';
    document.getElementById('deleteEquipmentModalOverlay').classList.add('open');
}

function closeDeleteEquipmentModal() {
    document.getElementById('deleteEquipmentModalOverlay').classList.remove('open');
    equipmentToDelete = null;
}

async function deleteEquipment() {
    if (!equipmentToDelete) return;

    const errorEl = document.getElementById('deleteEquipmentError');
    const btn = document.getElementById('confirmDeleteEquipmentBtn');
    errorEl.style.display = 'none';
    btn.disabled = true;
    btn.textContent = 'Deleting...';

    try {
        const response = await fetch('/Admin/DeleteEquipment', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ equipmentId: equipmentToDelete })
        });
        const result = await response.json();

        if (result.success) {
            const card = document.getElementById('equipment-' + equipmentToDelete);
            if (card) card.remove();
            closeDeleteEquipmentModal();
        } else {
            errorEl.textContent = result.message || 'Failed to delete equipment';
            errorEl.style.display = 'block';
        }
    } catch (err) {
        errorEl.textContent = 'Something went wrong. Please try again.';
        errorEl.style.display = 'block';
    } finally {
        btn.disabled = false;
        btn.textContent = 'Delete';
    }
}

document.addEventListener('DOMContentLoaded', function () {
    const overlays = ['equipmentModalOverlay', 'deleteEquipmentModalOverlay', 'deletePhotoModalOverlay'];
    overlays.forEach(id => {
        const el = document.getElementById(id);
        if (el) {
            el.addEventListener('click', function (e) {
                if (e.target === this) {
                    if (id === 'equipmentModalOverlay') closeEquipmentModal();
                    else if (id === 'deleteEquipmentModalOverlay') closeDeleteEquipmentModal();
                    else closeDeletePhotoModal();
                }
            });
        }
    });
});