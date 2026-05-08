export function showToast(message, type = 'info', duration = 5000) {
    const toastContainer = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;

    toastContainer.appendChild(toast);
    setTimeout(() => toast.classList.add('show'), 10);
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, duration);
}

export function renderMessageItem(data, currentUser) {
    const messagesList = document.getElementById('messagesList');
    const li = document.createElement('li');
    li.className = 'message-item';

    if (data.from === currentUser) {
        li.classList.add('message-own');
    } else {
        li.classList.add('message-other');
    }

    const fromDiv = document.createElement('div');
    fromDiv.className = 'message-from';
    fromDiv.textContent = data.from || 'System';

    const contentDiv = document.createElement('div');
    contentDiv.textContent = data.content || '';

    const timeDiv = document.createElement('div');
    timeDiv.className = 'message-time';
    timeDiv.textContent = data.timestamp ? new Date(data.timestamp).toLocaleTimeString() : new Date().toLocaleTimeString();

    li.appendChild(fromDiv);
    li.appendChild(contentDiv);
    li.appendChild(timeDiv);

    if (data.attachments && data.attachments.length > 0) {
        const attachmentsDiv = document.createElement('div');
        attachmentsDiv.className = 'message-attachments';
        data.attachments.forEach(attachment => {
            const attachmentLink = document.createElement('a');
            attachmentLink.href = attachment.url || '#';
            attachmentLink.textContent = attachment.name;
            attachmentLink.target = '_blank';
            attachmentsDiv.appendChild(attachmentLink);
        });
        li.appendChild(attachmentsDiv);
    }

    messagesList.appendChild(li);
    messagesList.scrollTop = messagesList.scrollHeight;
}

export function renderCurrentChatMessages(currentChat, getStoredMessages, currentUser) {
    const messagesList = document.getElementById('messagesList');
    messagesList.innerHTML = '';
    if (!currentChat) {
        return;
    }

    const stored = getStoredMessages(currentChat.type, currentChat.target);
    stored.forEach(message => renderMessageItem(message, currentUser));
}

export function updateUsersList(users, currentChat, selectChat) {
    const usersListElement = document.getElementById('usersList');
    usersListElement.innerHTML = '';
    const onlineCount = users.filter(u => u.online).length;
    document.getElementById('onlineCount').textContent = onlineCount;

    users.forEach(user => {
        const li = document.createElement('li');
        li.className = `user-item ${user.online ? 'online' : 'offline'}`;
        li.setAttribute('data-user-name', user.name);
        li.textContent = user.name;
        li.addEventListener('click', () => selectChat(user.name, user.name, 'user'));

        if (currentChat && currentChat.type === 'user' && currentChat.target === user.name) {
            li.classList.add('active');
        }

        usersListElement.appendChild(li);
    });
}

export function updateGroupsList(groups, currentChat, selectChat) {
    const groupsListElement = document.getElementById('groupsList');
    groupsListElement.innerHTML = '';

    groups.forEach(group => {
        const li = document.createElement('li');
        li.className = 'group-item';
        li.setAttribute('data-group-id', group.id);
        li.innerHTML = `
            <div class="group-info">
                <span>${group.name}</span>
                <span class="group-count">${group.members.length}</span>
            </div>
        `;
        li.addEventListener('click', () => selectChat(group.id, group.name, 'group'));

        if (currentChat && currentChat.type === 'group' && currentChat.target === group.id) {
            li.classList.add('active');
        }

        groupsListElement.appendChild(li);
    });
}

export function setChatHeader(name, type) {
    const chatTitle = document.getElementById('chatTitle');
    const icon = type === 'group' ? '👥' : '👤';
    chatTitle.textContent = `${icon} ${name}`;
}

export function clearMessages() {
    document.getElementById('messagesList').innerHTML = '';
}

export function enableInputArea() {
    document.getElementById('messageInput').disabled = false;
    document.getElementById('sendButton').disabled = false;
}

export function disableInputArea() {
    document.getElementById('messageInput').disabled = true;
    document.getElementById('sendButton').disabled = true;
    document.getElementById('messageInput').value = '';
}
