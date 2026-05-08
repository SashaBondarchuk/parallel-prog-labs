export function handleFileSelect(event) {
    const files = Array.from(event.target.files);
    return files.map(file => ({
        name: file.name,
        size: file.size,
        type: file.type,
        file: file,
        url: URL.createObjectURL(file)
    }));
}

export function updateAttachmentPreview(attachments) {
    const preview = document.getElementById('attachmentPreview');
    const list = document.getElementById('attachmentList');

    if (attachments.length > 0) {
        preview.style.display = 'block';
        list.innerHTML = '';

        attachments.forEach((attachment, index) => {
            const div = document.createElement('div');
            div.className = 'attachment-item';
            div.innerHTML = `
                <span>${attachment.name} (${formatFileSize(attachment.size)})</span>
                <span class="remove-attachment" data-index="${index}">×</span>
            `;
            list.appendChild(div);
        });
    } else {
        preview.style.display = 'none';
    }
}

export function removeAttachment(attachments, index) {
    return attachments.filter((_, i) => i !== index);
}

export function clearAttachments() {
    return [];
}

export function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}
