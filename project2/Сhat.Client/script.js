import { loadStoredMessages, saveCurrentChatState, getStoredMessages, storeMessageForChat, CHAT_STATE_KEY } from './storage.js';
import { showToast, renderMessageItem, renderCurrentChatMessages, updateUsersList, updateGroupsList, setChatHeader, clearMessages, enableInputArea, disableInputArea } from './ui.js';
import { connect as connectSocket, sendSystemMessage, isSocketOpen } from './websocket.js';
import { handleFileSelect, updateAttachmentPreview, removeAttachment, clearAttachments } from './attachments.js';

let currentUser = '';
let users = [];
let groups = [];
let currentChat = null;
let attachments = [];
let isConnected = false;

function deriveChatForIncoming(data) {
    if (data.groupId) {
        return {
            type: 'group',
            target: data.groupId,
            name: getGroupNameById(data.groupId) || data.groupId
        };
    }

    if (data.group && data.group.id) {
        return {
            type: 'group',
            target: data.group.id,
            name: data.group.name || getGroupNameById(data.group.id) || data.group.id
        };
    }

    if (data.from) {
        return { type: 'user', target: data.from, name: data.from };
    }

    if (currentChat && data.type === 'message') {
        return currentChat;
    }

    return null;
}

function getGroupNameById(groupId) {
    const group = groups.find(g => g.id === groupId);
    return group ? group.name : null;
}

function handleIncomingMessage(data) {
    const chat = deriveChatForIncoming(data);
    if (!chat) {
        showToast('Отримано повідомлення, яке не можна прив\'язати до чату', 'warning');
        return;
    }

    const message = {
        from: data.from || data.From || 'System',
        content: data.content || data.Content || '',
        attachments: data.attachments || [],
        timestamp: data.timestamp || new Date().toISOString()
    };

    storeMessageForChat(chat, message);

    if (isConnected && currentChat && currentChat.type === chat.type && currentChat.target === chat.target) {
        renderMessageItem(message, currentUser);
    } else if (chat.type === 'user') {
        showToast(`Нове повідомлення від ${message.from}`, 'info', 2500);
    } else {
        showToast(`Нове повідомлення у групі ${chat.name}`, 'info', 2500);
    }
}

function handleMessage(data) {
    if (data.From && data.Content) {
        handleIncomingMessage(data);
        return;
    }

    switch (data.type) {
        case 'users_list':
            users = data.users || [];
            updateUsersList(users, currentChat, selectChat);
            break;
        case 'groups_list':
            groups = data.groups || [];
            updateGroupsList(groups, currentChat, selectChat);
            break;
        case 'message':
            handleIncomingMessage(data);
            break;
        case 'group_created':
            groups.push(data.group);
            updateGroupsList(groups, currentChat, selectChat);
            break;
        case 'user_joined':
        case 'user_left':
            updateUserStatus(data);
            break;
        default:
            if (data.type) {
                handleIncomingMessage(data);
            }
    }
}

function updateUserStatus(data) {
    const userIndex = users.findIndex(u => u.name === data.user);
    if (userIndex !== -1) {
        users[userIndex].online = data.type === 'user_joined';
    } else {
        users.push({ name: data.user, online: true });
    }
    updateUsersList(users, currentChat, selectChat);
}

function selectChat(target, name, type = 'user') {
    currentChat = { type, target, name };
    saveCurrentChatState(currentChat);
    setChatHeader(name, type);
    if (isConnected) {
        renderCurrentChatMessages(currentChat, getStoredMessages, currentUser);
    }
    updateUsersList(users, currentChat, selectChat);
    updateGroupsList(groups, currentChat, selectChat);
    enableInputArea();
    showToast(`Обрано чат: ${name}`, 'info', 2000);
}

function deselectChat() {
    currentChat = null;
    saveCurrentChatState(null);
    setChatHeader('Оберіть користувача або групу для початку спілкування', 'none');
    clearMessages();
    updateUsersList(users, null, selectChat);
    updateGroupsList(groups, null, selectChat);
    disableInputArea();
}

function openCreateGroupModal() {
    document.getElementById('createGroupModal').style.display = 'block';
    populateUserCheckboxes();
}

function closeCreateGroupModal() {
    document.getElementById('createGroupModal').style.display = 'none';
}

function populateUserCheckboxes() {
    const container = document.getElementById('userCheckboxes');
    container.innerHTML = '';
    const availableUsers = users.filter(u => u.online && u.name !== currentUser);

    if (availableUsers.length === 0) {
        container.innerHTML = '<p style="padding: 10px; color: #6c757d;">Немає доступних користувачів для додавання</p>';
        return;
    }

    availableUsers.forEach(user => {
        const div = document.createElement('div');
        div.className = 'checkbox-item';
        div.innerHTML = `
            <input type="checkbox" id="user_${user.name}" value="${user.name}">
            <label for="user_${user.name}">${user.name}</label>
        `;
        container.appendChild(div);
    });
}

function createGroup() {
    const groupName = document.getElementById('groupName').value.trim();
    if (!groupName) {
        showToast('Будь ласка, введіть назву групи', 'warning');
        return;
    }

    const selectedUsers = Array.from(document.querySelectorAll('#userCheckboxes input:checked')).map(cb => cb.value);
    if (selectedUsers.length === 0) {
        showToast('Будь ласка, оберіть хоча б одного користувача', 'warning');
        return;
    }

    const groupMessage = {
        type: 'create_group',
        name: groupName,
        members: selectedUsers
    };

    try {
        sendSystemMessage(groupMessage);
        closeCreateGroupModal();
        document.getElementById('groupName').value = '';
    } catch (error) {
        showToast('Помилка створення групи: ' + error.message, 'error');
    }
}

function sendMessage() {
    if (!isSocketOpen()) {
        showToast('Немає з\'єднання з сервером', 'error');
        return;
    }

    const content = document.getElementById('messageInput').value.trim();
    if (!content && attachments.length === 0) {
        showToast('Введіть повідомлення або додайте файл', 'warning');
        return;
    }

    if (!currentChat) {
        showToast('Оберіть отримувача або групу', 'warning');
        return;
    }

    const message = {
        type: 'message',
        content: content,
        to: currentChat.target,
        toType: currentChat.type,
        attachments: attachments.map(att => ({
            name: att.name,
            size: att.size,
            type: att.type,
            url: att.url
        })),
        timestamp: new Date().toISOString()
    };

    try {
        sendSystemMessage(message);
        const savedMessage = {
            from: currentUser,
            content: content,
            attachments: message.attachments,
            timestamp: message.timestamp
        };
        storeMessageForChat(currentChat, savedMessage);
        renderMessageItem(savedMessage, currentUser);
        document.getElementById('messageInput').value = '';
        attachments = clearAttachments();
        updateAttachmentPreview(attachments);
    } catch (error) {
        showToast('Помилка відправки повідомлення: ' + error.message, 'error');
    }
}

function setupEventHandlers() {
    document.getElementById('connectButton').addEventListener('click', connect);
    document.getElementById('sendButton').addEventListener('click', sendMessage);
    document.getElementById('createGroupBtn').addEventListener('click', openCreateGroupModal);
    document.getElementById('createGroupButton').addEventListener('click', createGroup);
    document.querySelector('.close').addEventListener('click', closeCreateGroupModal);
    document.getElementById('fileInput').addEventListener('change', event => {
        attachments = attachments.concat(handleFileSelect(event));
        updateAttachmentPreview(attachments);
    });
    document.querySelector('.file-button').addEventListener('click', () => document.getElementById('fileInput').click());
    document.getElementById('messageInput').addEventListener('keypress', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });
    document.getElementById('attachmentList').addEventListener('click', event => {
        if (event.target.classList.contains('remove-attachment')) {
            const index = Number(event.target.dataset.index);
            attachments = removeAttachment(attachments, index);
            updateAttachmentPreview(attachments);
        }
    });
    window.addEventListener('click', function (event) {
        const modal = document.getElementById('createGroupModal');
        if (event.target === modal) {
            closeCreateGroupModal();
        }
    });
}

function connect() {
    const user = document.getElementById('username').value.trim();
    if (!user) {
        showToast('Будь ласка, введіть ім\'я користувача', 'warning');
        return;
    }

    currentUser = user;
    connectSocket(currentUser, {
        onOpen() {
            localStorage.removeItem(CHAT_STATE_KEY);

            isConnected = true;
            showToast('Підключено до сервера', 'success');
            document.getElementById('connectButton').textContent = 'Connected';
            document.getElementById('connectButton').classList.add('connected');
            document.getElementById('username').disabled = true;
            document.getElementById('connectButton').disabled = true;
            sendSystemMessage({ type: 'get_users' });
            sendSystemMessage({ type: 'get_groups' });
            // Render messages if a chat was previously selected
            initializeFromStorage();
            if (currentChat) {
                renderCurrentChatMessages(currentChat, getStoredMessages, currentUser);
            }
        },
        onMessage(data) {
            handleMessage(data);
        },
        onClose(event) {
            isConnected = false;
            let message = 'З\'єднання закрито';
            if (event.code !== 1000) {
                message += ` (код: ${event.code})`;
                showToast(message, 'error');
            }
            document.getElementById('connectButton').textContent = 'Connect';
            document.getElementById('connectButton').classList.remove('connected');
            document.getElementById('username').disabled = false;
            document.getElementById('connectButton').disabled = false;
            // Clear messages and disable input when disconnected
            clearMessages();
            disableInputArea();
            localStorage.removeItem(CHAT_STATE_KEY);
        },
        onError(error) {
            showToast('Помилка WebSocket: ' + error.message, 'error');
        }
    });
}

function initializeFromStorage() {
    const storedChat = loadStoredMessages();
    if (storedChat) {
        if (isConnected) {
            currentChat = storedChat;
            setChatHeader(currentChat.name, currentChat.type);
            renderCurrentChatMessages(currentChat, getStoredMessages, currentUser);
            enableInputArea();
        }
    } else {
        deselectChat();
    }
}

document.addEventListener('DOMContentLoaded', function () {
    setupEventHandlers();
});
