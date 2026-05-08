const STORAGE_KEY = 'chat_messages_v1';
export const CHAT_STATE_KEY = 'last_chat_state_v1';
const ATTACHMENTS_STORAGE_KEY = 'chat_attachments_v1';
let messagesByChat = {};
let attachmentsStorage = {};

export function loadStoredMessages() {
    try {
        const stored = localStorage.getItem(STORAGE_KEY);
        messagesByChat = stored ? JSON.parse(stored) : {};
    } catch (error) {
        messagesByChat = {};
    }

    try {
        const storedAttachments = sessionStorage.getItem(ATTACHMENTS_STORAGE_KEY);
        attachmentsStorage = storedAttachments ? JSON.parse(storedAttachments) : {};
    } catch (error) {
        attachmentsStorage = {};
    }

    try {
        const storedState = localStorage.getItem(CHAT_STATE_KEY);
        const parsedState = storedState ? JSON.parse(storedState) : null;
        return parsedState && parsedState.type && parsedState.target ? parsedState : null;
    } catch (error) {
        return null;
    }
}

function getChatKey(type, target) {
    return `${type}:${target}`;
}

export function persistStoredMessages() {
    try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(messagesByChat));
    } catch (error) {
        console.warn('Could not persist messages:', error);
    }
}

function persistAttachmentsStorage() {
    try {
        localStorage.setItem(ATTACHMENTS_STORAGE_KEY, JSON.stringify(attachmentsStorage));
    } catch (error) {
        console.warn('Could not persist attachments:', error);
    }
}

export function saveCurrentChatState(currentChat) {
    if (!currentChat) {
        localStorage.removeItem(CHAT_STATE_KEY);
        return;
    }

    try {
        localStorage.setItem(CHAT_STATE_KEY, JSON.stringify(currentChat));
    } catch (error) {
        console.warn('Could not persist chat state:', error);
    }
}

export function getStoredMessages(type, target) {
    const key = getChatKey(type, target);
    return messagesByChat[key] || [];
}

export function storeMessageForChat(chat, message) {
    const key = getChatKey(chat.type, chat.target);
    if (!messagesByChat[key]) {
        messagesByChat[key] = [];
    }

    // Store attachments with file data URLs in sessionStorage
    if (message.attachments && message.attachments.length > 0) {
        message.attachments.forEach((attachment, index) => {
            const attachmentKey = `${key}:${messagesByChat[key].length}:${index}`;
            if (attachment.url && attachment.url.startsWith('blob:')) {
                attachmentsStorage[attachmentKey] = attachment.url;
            }
        });
        persistAttachmentsStorage();
    }

    messagesByChat[key].push(message);
    persistStoredMessages();
}

export function getAttachmentUrl(chatType, chatTarget, messageIndex, attachmentIndex) {
    const key = getChatKey(chatType, chatTarget);
    const attachmentKey = `${key}:${messageIndex}:${attachmentIndex}`;
    return attachmentsStorage[attachmentKey] || null;
}
