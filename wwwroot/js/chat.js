let currentContactId = null;
let chatPollInterval = null;

function initChat(contactId) {
    currentContactId = contactId;

    loadMessages();
    chatPollInterval = setInterval(loadMessages, 4000);

    const form = document.getElementById('chatForm');
    if (form) {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            await sendMessage();
        });
    }
}

async function loadMessages() {
    if (!currentContactId) return;

    try {
        const response = await fetch('/Chat/GetMessages?contactId=' + currentContactId);
        const result = await response.json();

        if (result.success) {
            renderMessages(result.messages);
        }
    } catch (err) {
        // Fail silently, will retry on next poll
    }
}

function renderMessages(messages) {
    const container = document.getElementById('chatMessages');
    if (!container) return;

    const wasAtBottom = container.scrollTop + container.clientHeight >= container.scrollHeight - 20;

    container.innerHTML = messages.map(m => `
        <div class="chat-bubble-row ${m.isMine ? 'mine' : ''}">
            <div class="chat-bubble">
                <div class="chat-bubble-text">${escapeHtml(m.message)}</div>
                <div class="chat-bubble-footer">
                    <span class="chat-bubble-time">${formatTime(m.sentAt)}</span>
                    ${m.isMine ? `<button class="chat-delete-btn" onclick="deleteMessage(${m.chatMessageId})" title="Delete">❌</button>` : ''}
                </div>
            </div>
        </div>
    `).join('');

    if (wasAtBottom || messages.length <= 1) {
        container.scrollTop = container.scrollHeight;
    }
}

async function deleteMessage(messageId) {
    if (!confirm('Delete this message?')) return;

    try {
        const response = await fetch('/Chat/DeleteMessage', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ chatMessageId: messageId })
        });
        const result = await response.json();

        if (result.success) {
            loadMessages();
        } else {
            alert(result.message || 'Failed to delete message');
        }
    } catch (err) {
        alert('Something went wrong. Please try again.');
    }
}

function formatTime(dateStr) {
    const date = new Date(dateStr);
    return date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

async function sendMessage() {
    const input = document.getElementById('MessageInput');
    const message = input.value.trim();
    if (!message || !currentContactId) return;

    input.value = '';

    try {
        const response = await fetch('/Chat/SendMessage', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ receiverId: currentContactId, message: message })
        });
        const result = await response.json();

        if (result.success) {
            loadMessages();
        } else {
            alert(result.message || 'Failed to send message');
        }
    } catch (err) {
        alert('Something went wrong. Please try again.');
    }
}

window.addEventListener('beforeunload', function () {
    if (chatPollInterval) clearInterval(chatPollInterval);
});