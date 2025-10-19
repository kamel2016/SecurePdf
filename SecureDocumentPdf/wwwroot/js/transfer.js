/**
 * Service de Transfert Securise - JavaScript
 */

// Variables globales
let selectedFile = null;
let transferData = null;

// Configuration API
const API_URL = '/api/filetransfer';

// Elements DOM
const elements = {
    // Upload
    uploadSection: document.getElementById('uploadSection'),
    dropZone: document.getElementById('dropZone'),
    fileInput: document.getElementById('fileInput'),
    fileSelected: document.getElementById('fileSelected'),
    fileName: document.getElementById('fileName'),
    fileSize: document.getElementById('fileSize'),
    btnRemoveFile: document.getElementById('btnRemoveFile'),
    transferForm: document.getElementById('transferForm'),
    btnSendFile: document.getElementById('btnSendFile'),

    // Progress
    progressSection: document.getElementById('progressSection'),
    progressText: document.getElementById('progressText'),
    progressFill: document.getElementById('progressFill'),
    progressPercent: document.getElementById('progressPercent'),

    // Success
    successSection: document.getElementById('successSection'),
    shareLink: document.getElementById('shareLink'),
    btnCopyLink: document.getElementById('btnCopyLink'),
    detailFileName: document.getElementById('detailFileName'),
    detailFileSize: document.getElementById('detailFileSize'),
    detailExpiration: document.getElementById('detailExpiration'),
    detailMaxDownloads: document.getElementById('detailMaxDownloads'),
    detailPassword: document.getElementById('detailPassword'),
    btnNewTransfer: document.getElementById('btnNewTransfer'),
    btnViewStats: document.getElementById('btnViewStats'),

    // Download
    downloadSection: document.getElementById('downloadSection'),
    dlFileName: document.getElementById('dlFileName'),
    dlFileSize: document.getElementById('dlFileSize'),
    dlSenderName: document.getElementById('dlSenderName'),
    dlExpiresAt: document.getElementById('dlExpiresAt'),
    dlDownloads: document.getElementById('dlDownloads'),
    dlMessageBox: document.getElementById('dlMessageBox'),
    dlMessage: document.getElementById('dlMessage'),
    passwordBox: document.getElementById('passwordBox'),
    downloadPassword: document.getElementById('downloadPassword'),
    btnDownload: document.getElementById('btnDownload')
};

// ============================================
// INITIALISATION
// ============================================

document.addEventListener('DOMContentLoaded', () => {
    initializeEventListeners();
    checkDownloadMode();
});

function initializeEventListeners() {
    // Upload events
    elements.fileInput.addEventListener('change', handleFileSelect);
    elements.btnRemoveFile.addEventListener('click', removeFile);
    elements.transferForm.addEventListener('submit', handleSubmit);
    elements.btnNewTransfer.addEventListener('click', resetTransfer);
    elements.btnCopyLink.addEventListener('click', copyShareLink);

    // Drag & Drop
    elements.dropZone.addEventListener('dragover', handleDragOver);
    elements.dropZone.addEventListener('dragleave', handleDragLeave);
    elements.dropZone.addEventListener('drop', handleDrop);

    // Download events
    if (elements.btnDownload) {
        elements.btnDownload.addEventListener('click', handleDownload);
    }
}

// ============================================
// GESTION UPLOAD
// ============================================

function handleFileSelect(event) {
    const file = event.target.files[0];
    if (file) {
        selectFile(file);
    }
}

function handleDragOver(event) {
    event.preventDefault();
    elements.dropZone.classList.add('drag-over');
}

function handleDragLeave(event) {
    event.preventDefault();
    elements.dropZone.classList.remove('drag-over');
}

function handleDrop(event) {
    event.preventDefault();
    elements.dropZone.classList.remove('drag-over');

    const file = event.dataTransfer.files[0];
    if (file) {
        selectFile(file);
    }
}

function selectFile(file) {
    // Verifier la taille (2 GB max)
    const maxSize = 2 * 1024 * 1024 * 1024; // 2 GB
    if (file.size > maxSize) {
        alert('Fichier trop volumineux. Taille maximale : 2 GB');
        return;
    }

    selectedFile = file;

    // Afficher les infos du fichier
    elements.fileName.textContent = file.name;
    elements.fileSize.textContent = formatFileSize(file.size);

    // Afficher la section fichier selectionne
    elements.fileSelected.style.display = 'block';
    elements.transferForm.style.display = 'block';
    elements.dropZone.style.display = 'none';
}

function removeFile() {
    selectedFile = null;
    elements.fileInput.value = '';
    elements.fileSelected.style.display = 'none';
    elements.transferForm.style.display = 'none';
    elements.dropZone.style.display = 'block';
}

async function handleSubmit(event) {
    event.preventDefault();

    if (!selectedFile) {
        alert('Aucun fichier selectionne');
        return;
    }

    // Recuperer les valeurs du formulaire
    const senderEmail = document.getElementById('senderEmail').value;
    const senderName = document.getElementById('senderName').value;
    const recipientEmail = document.getElementById('recipientEmail').value;
    const message = document.getElementById('message').value;
    const expirationHours = document.getElementById('expirationHours').value;
    const maxDownloads = document.getElementById('maxDownloads').value;
    const password = document.getElementById('password').value;

    // Validation
    if (!senderEmail) {
        alert('Votre email est requis');
        return;
    }

    // Preparer les donnees
    const formData = new FormData();
    formData.append('file', selectedFile);
    formData.append('senderEmail', senderEmail);
    formData.append('senderName', senderName || senderEmail);
    formData.append('recipientEmail', recipientEmail);
    formData.append('message', message);
    formData.append('expirationHours', expirationHours);
    formData.append('maxDownloads', maxDownloads);
    formData.append('password', password);

    // Afficher la progression
    showSection('progress');
    updateProgress(0, 'Chiffrement du fichier...');

    try {
        // Envoyer la requete
        const xhr = new XMLHttpRequest();

        xhr.upload.addEventListener('progress', (e) => {
            if (e.lengthComputable) {
                const percent = Math.round((e.loaded / e.total) * 100);
                updateProgress(percent, 'Upload en cours...');
            }
        });

        xhr.addEventListener('load', () => {
            if (xhr.status === 200) {
                const response = JSON.parse(xhr.responseText);
                if (response.success) {
                    transferData = response;
                    showSuccess(response);
                } else {
                    alert('Erreur : ' + response.errorMessage);
                    showSection('upload');
                }
            } else {
                alert('Erreur serveur : ' + xhr.status);
                showSection('upload');
            }
        });

        xhr.addEventListener('error', () => {
            alert('Erreur de connexion');
            showSection('upload');
        });

        xhr.open('POST', `${API_URL}/create`);
        xhr.send(formData);

    } catch (error) {
        console.error('Erreur:', error);
        alert('Erreur : ' + error.message);
        showSection('upload');
    }
}

function updateProgress(percent, text) {
    elements.progressFill.style.width = percent + '%';
    elements.progressPercent.textContent = percent + '%';
    elements.progressText.textContent = text;
}

function showSuccess(response) {
    showSection('success');

    // Afficher le lien de partage
    elements.shareLink.value = response.shareUrl;

    // Afficher les details
    elements.detailFileName.textContent = selectedFile.name;
    elements.detailFileSize.textContent = formatFileSize(selectedFile.size);
    elements.detailExpiration.textContent = new Date(response.expiresAt).toLocaleString('fr-FR');

    const maxDl = document.getElementById('maxDownloads').value;
    elements.detailMaxDownloads.textContent = maxDl === '999999' ? 'Illimite' : maxDl;

    const password = document.getElementById('password').value;
    elements.detailPassword.textContent = password ? 'Oui' : 'Non';
}

function copyShareLink() {
    elements.shareLink.select();
    document.execCommand('copy');

    // Feedback visuel
    const originalText = elements.btnCopyLink.textContent;
    elements.btnCopyLink.textContent = '✅ Copie !';
    setTimeout(() => {
        elements.btnCopyLink.textContent = originalText;
    }, 2000);
}

function resetTransfer() {
    selectedFile = null;
    transferData = null;
    elements.transferForm.reset();
    elements.fileInput.value = '';
    showSection('upload');
    removeFile();
}

// ============================================
// GESTION DOWNLOAD
// ============================================

//function checkDownloadMode() {
//    const urlParams = new URLSearchParams(window.location.search);
//    const transferId = getTransferIdFromUrl();
//    const token = urlParams.get('token');

//    if (transferId && token) {
//        loadTransferInfo(transferId, token);
//    }
//}

function checkDownloadMode() {
    console.log('checkDownloadMode appelé');
    console.log('window.transferParams:', window.transferParams);
    console.log('URL:', window.location.search);

    // Methode 1 : Depuis les parametres passes par Razor
    if (window.transferParams && window.transferParams.transferId && window.transferParams.token) {
        console.log('Mode download détecté (Razor params)');
        loadTransferInfo(window.transferParams.transferId, window.transferParams.token);
        return;
    }

    // Methode 2 : Depuis l'URL
    const urlParams = new URLSearchParams(window.location.search);
    const transferId = urlParams.get('transferId');
    const token = urlParams.get('token');

    console.log('transferId:', transferId);
    console.log('token:', token);

    if (transferId && token) {
        console.log('Mode download détecté (URL params)');
        loadTransferInfo(transferId, token);
    } else {
        console.log('Mode upload');
    }
}

function getTransferIdFromUrl() {
    const path = window.location.pathname;
    const match = path.match(/\/Transfer\/Download\/([^\/]+)/);
    return match ? match[1] : null;
}

async function loadTransferInfo(transferId, token) {
    try {
        showSection('download');

        const response = await fetch(`${API_URL}/info/${transferId}?token=${token}`);

        if (!response.ok) {
            throw new Error('Transfert introuvable ou expire');
        }

        const info = await response.json();

        // Afficher les informations
        elements.dlFileName.textContent = info.fileName;
        elements.dlFileSize.textContent = info.fileSizeFormatted;
        elements.dlSenderName.textContent = info.senderName || 'Anonyme';
        elements.dlExpiresAt.textContent = new Date(info.expiresAt).toLocaleString('fr-FR');
        elements.dlDownloads.textContent = `${info.currentDownloads} / ${info.maxDownloads}`;

        // Message
        if (info.message) {
            elements.dlMessageBox.style.display = 'block';
            elements.dlMessage.textContent = info.message;
        }

        // Mot de passe
        if (info.requiresPassword) {
            elements.passwordBox.style.display = 'block';
        }

        // Verifier si expire
        if (info.isExpired) {
            alert('Ce transfert a expire');
            elements.btnDownload.disabled = true;
        }

        // Stocker les infos pour le telechargement
        elements.btnDownload.dataset.transferId = transferId;
        elements.btnDownload.dataset.token = token;

    } catch (error) {
        console.error('Erreur:', error);
        alert('Erreur : ' + error.message);
    }
}

async function handleDownload() {
    const transferId = elements.btnDownload.dataset.transferId;
    const token = elements.btnDownload.dataset.token;
    const password = elements.downloadPassword.value;

    try {
        elements.btnDownload.disabled = true;
        elements.btnDownload.textContent = 'Telechargement en cours...';

        const response = await fetch(`${API_URL}/download`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                transferId: transferId,
                accessToken: token,
                password: password
            })
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Erreur lors du telechargement');
        }

        // Recuperer le fichier
        const blob = await response.blob();
        const filename = getFilenameFromHeaders(response.headers) || 'fichier';

        // Telecharger
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);

        // Succes
        elements.btnDownload.textContent = '✅ Telechargement reussi !';
        setTimeout(() => {
            elements.btnDownload.textContent = '⬇️ Telecharger le fichier';
            elements.btnDownload.disabled = false;
        }, 3000);

    } catch (error) {
        console.error('Erreur:', error);
        alert('Erreur : ' + error.message);
        elements.btnDownload.textContent = '⬇️ Telecharger le fichier';
        elements.btnDownload.disabled = false;
    }
}

function getFilenameFromHeaders(headers) {
    const disposition = headers.get('content-disposition');
    if (disposition) {
        const match = disposition.match(/filename="?(.+)"?/);
        if (match) {
            return match[1];
        }
    }
    return null;
}

// ============================================
// UTILITAIRES
// ============================================

function showSection(section) {
    elements.uploadSection.style.display = section === 'upload' ? 'block' : 'none';
    elements.progressSection.style.display = section === 'progress' ? 'block' : 'none';
    elements.successSection.style.display = section === 'success' ? 'block' : 'none';
    elements.downloadSection.style.display = section === 'download' ? 'block' : 'none';
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
}