/**
 * Visualiseur PDF Sécurisé - JavaScript
 * Gère l'upload, la validation et l'affichage des PDF sécurisés
 */

// Variables globales
let selectedFile = null;
let pdfDocument = null;
let currentPage = 1;
let currentZoom = 1.0;
let validationResult = null;
let permissions = null;

// Configuration API
//const API_URL = window.API_BASE_URL || '/api/pdfviewer';
const API_URL = '/api/pdfviewer';

// Éléments DOM
const elements = {
    // Upload
    fileInput: document.getElementById('pdfFileInput'),
    fileName: document.getElementById('fileName'),
    fileSize: document.getElementById('fileSize'),
    fileInfo: document.getElementById('fileInfo'),
    validateBtn: document.getElementById('validateBtn'),

    // Options avancées
    toggleAdvanced: document.getElementById('toggleAdvanced'),
    advancedContent: document.getElementById('advancedContent'),
    expectedHash: document.getElementById('expectedHash'),
    userPassword: document.getElementById('userPassword'),

    // Sections
    uploadSection: document.getElementById('uploadSection'),
    validationSection: document.getElementById('validationSection'),
    resultsSection: document.getElementById('resultsSection'),
    viewerSection: document.getElementById('viewerSection'),

    // Validation
    validationStatus: document.getElementById('validationStatus'),
    step1: document.getElementById('step1'),
    step2: document.getElementById('step2'),
    step3: document.getElementById('step3'),
    step4: document.getElementById('step4'),

    // Résultats
    successCard: document.getElementById('successCard'),
    errorCard: document.getElementById('errorCard'),
    securityInfoGrid: document.getElementById('securityInfoGrid'),
    permissionsGrid: document.getElementById('permissionsGrid'),
    errorMessage: document.getElementById('errorMessage'),
    errorDetails: document.getElementById('errorDetails'),
    viewPdfBtn: document.getElementById('viewPdfBtn'),
    retryBtn: document.getElementById('retryBtn'),

    // Visualiseur
    closeViewerBtn: document.getElementById('closeViewerBtn'),
    documentTitle: document.getElementById('documentTitle'),
    pdfCanvas: document.getElementById('pdfCanvas'),
    currentPageSpan: document.getElementById('currentPage'),
    totalPagesSpan: document.getElementById('totalPages'),
    zoomLevel: document.getElementById('zoomLevel'),
    prevPageBtn: document.getElementById('prevPageBtn'),
    nextPageBtn: document.getElementById('nextPageBtn'),
    zoomInBtn: document.getElementById('zoomInBtn'),
    zoomOutBtn: document.getElementById('zoomOutBtn'),
    printBtn: document.getElementById('printBtn'),
    downloadBtn: document.getElementById('downloadBtn'),
    securityBanner: document.getElementById('securityBanner'),
    securityBannerText: document.getElementById('securityBannerText'),
    viewerWatermark: document.getElementById('viewerWatermark'),
    copyProtectionOverlay: document.getElementById('copyProtectionOverlay')
};

// ============================================
// INITIALISATION
// ============================================

document.addEventListener('DOMContentLoaded', () => {
    initializeEventListeners();
    applySecurityMeasures();
});

function initializeEventListeners() {
    // Upload
    elements.fileInput.addEventListener('change', handleFileSelect);
    elements.validateBtn.addEventListener('click', handleValidate);

    // Options avancées
    elements.toggleAdvanced.addEventListener('click', toggleAdvancedOptions);

    // Résultats
    elements.viewPdfBtn.addEventListener('click', showPdfViewer);
    elements.retryBtn.addEventListener('click', resetViewer);

    // Visualiseur
    elements.closeViewerBtn.addEventListener('click', closePdfViewer);
    elements.prevPageBtn.addEventListener('click', () => changePage(-1));
    elements.nextPageBtn.addEventListener('click', () => changePage(1));
    elements.zoomInBtn.addEventListener('click', () => changeZoom(0.1));
    elements.zoomOutBtn.addEventListener('click', () => changeZoom(-0.1));
    elements.printBtn.addEventListener('click', handlePrint);
    elements.downloadBtn.addEventListener('click', handleDownload);
}

// ============================================
// GESTION DE L'UPLOAD
// ============================================

function handleFileSelect(event) {
    const file = event.target.files[0];

    if (!file) {
        selectedFile = null;
        elements.validateBtn.disabled = true;
        elements.fileInfo.style.display = 'none';
        return;
    }

    // Vérifier l'extension
    if (!file.name.toLowerCase().endsWith('.pdf')) {
        alert('⚠️ Veuillez sélectionner un fichier PDF');
        event.target.value = '';
        return;
    }

    // Vérifier la taille (max 50 MB)
    const maxSize = 50 * 1024 * 1024; // 50 MB
    if (file.size > maxSize) {
        alert('⚠️ Le fichier est trop volumineux (max 50 MB)');
        event.target.value = '';
        return;
    }

    selectedFile = file;
    elements.validateBtn.disabled = false;

    // Afficher les infos du fichier
    elements.fileName.textContent = file.name;
    elements.fileSize.textContent = formatFileSize(file.size);
    elements.fileInfo.style.display = 'flex';
}

function toggleAdvancedOptions() {
    const content = elements.advancedContent;
    const isVisible = content.style.display !== 'none';

    content.style.display = isVisible ? 'none' : 'block';
    elements.toggleAdvanced.textContent = isVisible
        ? 'Options avancées ▼'
        : 'Options avancées ▲';
}

// ============================================
// VALIDATION DU PDF
// ============================================

async function handleValidate() {
    if (!selectedFile) {
        alert('⚠️ Aucun fichier sélectionné');
        return;
    }

    // Afficher la section de validation
    showSection('validation');
    resetValidationSteps();

    try {
        // Préparer les données du formulaire
        const formData = new FormData();
        formData.append('file', selectedFile);

        const expectedHash = elements.expectedHash.value.trim();
        if (expectedHash) {
            formData.append('expectedHash', expectedHash);
        }

        const userPassword = elements.userPassword.value;
        if (userPassword) {
            formData.append('userPassword', userPassword);
        }

        // Étape 1: Envoi du fichier
        updateStep(1, 'loading', 'Envoi du fichier...');

        const response = await fetch(`${API_URL}/upload`, {
            method: 'POST',
            body: formData
        });

        updateStep(1, 'success', 'Format vérifié');

        // Étape 2: Réception de la réponse
        updateStep(2, 'loading', 'Vérification de l\'intégrité...');

        if (!response.ok) {
            throw new Error(`Erreur HTTP: ${response.status}`);
        }

        const result = await response.json();
        validationResult = result.validationResult;

        updateStep(2, 'success', 'Intégrité vérifiée');

        // Étape 3: Extraction des permissions
        updateStep(3, 'loading', 'Extraction des permissions...');
        await sleep(500); // Simuler un délai

        permissions = validationResult.permissions;
        updateStep(3, 'success', 'Permissions extraites');

        // Étape 4: Validation finale
        updateStep(4, 'loading', 'Génération du token...');
        await sleep(500);

        if (result.canView) {
            updateStep(4, 'success', 'Token généré');
            await sleep(500);
            showValidationResults(true, result);
        } else {
            updateStep(4, 'error', 'Validation échouée');
            await sleep(500);
            showValidationResults(false, result);
        }

    } catch (error) {
        console.error('Erreur validation:', error);
        updateStep(1, 'error', 'Erreur');
        updateStep(2, 'error', 'Erreur');
        updateStep(3, 'error', 'Erreur');
        updateStep(4, 'error', 'Erreur');

        await sleep(1000);
        showValidationResults(false, {
            errorMessage: `Erreur technique: ${error.message}`
        });
    }
}

function resetValidationSteps() {
    [1, 2, 3, 4].forEach(step => {
        updateStep(step, 'pending', '');
    });
}

function updateStep(stepNumber, status, text) {
    const step = elements[`step${stepNumber}`];
    const icon = step.querySelector('.step-icon');
    const textSpan = step.querySelector('span:last-child');

    // Retirer les anciennes classes
    step.classList.remove('pending', 'loading', 'success', 'error');
    step.classList.add(status);

    // Mettre à jour l'icône
    const icons = {
        pending: '⏳',
        loading: '🔄',
        success: '✅',
        error: '❌'
    };
    icon.textContent = icons[status] || '⏳';

    // Mettre à jour le texte si fourni
    if (text) {
        textSpan.textContent = text;
    }
}

// ============================================
// AFFICHAGE DES RÉSULTATS
// ============================================

function showValidationResults(success, result) {
    showSection('results');

    if (success) {
        elements.successCard.style.display = 'block';
        elements.errorCard.style.display = 'none';

        // Afficher les infos de sécurité
        displaySecurityInfo(result.validationResult);

        // Afficher les permissions
        displayPermissions(result.validationResult.permissions);

    } else {
        elements.successCard.style.display = 'none';
        elements.errorCard.style.display = 'block';

        // Afficher l'erreur
        elements.errorMessage.textContent = result.errorMessage || 'Erreur inconnue';

        // Afficher les détails d'erreur
        if (result.validationResult && result.validationResult.securityIssues) {
            const issues = result.validationResult.securityIssues;
            elements.errorDetails.innerHTML = `
                <h4>Problèmes détectés :</h4>
                <ul>
                    ${issues.map(issue => `<li>❌ ${issue}</li>`).join('')}
                </ul>
            `;
        }
    }
}

function displaySecurityInfo(validation) {
    const info = [
        {
            label: 'Intégrité',
            value: validation.hashVerified ? '✅ Vérifiée' : '❌ Non vérifiée',
            class: validation.hashVerified ? 'success' : 'warning'
        },
        {
            label: 'Corruption',
            value: validation.isCorrupted ? '❌ Fichier corrompu' : '✅ Fichier sain',
            class: validation.isCorrupted ? 'error' : 'success'
        },
        {
            label: 'Chiffrement',
            value: validation.isEncrypted ? `🔒 ${validation.encryptionLevel}` : 'Non chiffré',
            class: validation.isEncrypted ? 'success' : 'warning'
        },
        {
            label: 'Taille',
            value: formatFileSize(validation.fileSize),
            class: 'info'
        }
    ];

    elements.securityInfoGrid.innerHTML = info.map(item => `
        <div class="info-item ${item.class}">
            <span class="info-label">${item.label}:</span>
            <span class="info-value">${item.value}</span>
        </div>
    `).join('');
}

function displayPermissions(perms) {
    const permissionsList = [
        { label: 'Impression', value: perms.allowPrinting, icon: '🖨️' },
        { label: 'Copie', value: perms.allowCopy, icon: '📋' },
        { label: 'Modification', value: perms.allowModifyContents, icon: '✏️' },
        { label: 'Annotations', value: perms.allowModifyAnnotations, icon: '📝' },
        { label: 'Formulaires', value: perms.allowFillIn, icon: '📄' },
        { label: 'Lecture d\'écran', value: perms.allowScreenReaders, icon: '🔊' }
    ];

    elements.permissionsGrid.innerHTML = permissionsList.map(perm => `
        <div class="permission-item ${perm.value ? 'allowed' : 'denied'}">
            <span class="perm-icon">${perm.icon}</span>
            <span class="perm-label">${perm.label}</span>
            <span class="perm-status">${perm.value ? '✅' : '❌'}</span>
        </div>
    `).join('');
}

// ============================================
// VISUALISEUR PDF
// ============================================

async function showPdfViewer() {
    if (!validationResult || !validationResult.isValid) {
        alert('❌ Le document n\'est pas valide');
        return;
    }

    showSection('viewer');

    // Mettre à jour le titre
    elements.documentTitle.textContent = selectedFile.name;

    // Configurer les contrôles selon les permissions
    elements.printBtn.disabled = !permissions.allowPrinting;
    elements.downloadBtn.disabled = true; // Toujours désactivé pour la sécurité

    // Afficher la bannière de sécurité
    updateSecurityBanner();

    // Afficher le watermark si nécessaire
    if (validationResult.hasWatermark || !permissions.allowCopy) {
        elements.viewerWatermark.style.display = 'block';
    }

    // Activer la protection contre la copie si nécessaire
    if (!permissions.allowCopy) {
        elements.copyProtectionOverlay.style.display = 'block';
    }

    // Charger et afficher le PDF
    await loadAndRenderPdf();
}

async function loadAndRenderPdf() {
    try {
        // Lire le fichier
        const arrayBuffer = await selectedFile.arrayBuffer();

        // Charger le PDF avec PDF.js
        const loadingTask = pdfjsLib.getDocument({ data: arrayBuffer });
        pdfDocument = await loadingTask.promise;

        // Mettre à jour le nombre total de pages
        elements.totalPagesSpan.textContent = pdfDocument.numPages;

        // Afficher la première page
        currentPage = 1;
        await renderPage(currentPage);

    } catch (error) {
        console.error('Erreur chargement PDF:', error);
        alert(`❌ Erreur lors du chargement du PDF: ${error.message}`);
        closePdfViewer();
    }
}

async function renderPage(pageNumber) {
    try {
        // Récupérer la page
        const page = await pdfDocument.getPage(pageNumber);

        const canvas = elements.pdfCanvas;
        const context = canvas.getContext('2d');

        // Calculer la taille avec le zoom
        const viewport = page.getViewport({ scale: currentZoom });

        canvas.width = viewport.width;
        canvas.height = viewport.height;

        // Rendre la page
        const renderContext = {
            canvasContext: context,
            viewport: viewport
        };

        await page.render(renderContext).promise;

        // Mettre à jour l'interface
        elements.currentPageSpan.textContent = currentPage;
        updateNavigationButtons();

    } catch (error) {
        console.error('Erreur rendu page:', error);
        alert(`❌ Erreur lors du rendu de la page: ${error.message}`);
    }
}

function updateNavigationButtons() {
    elements.prevPageBtn.disabled = currentPage <= 1;
    elements.nextPageBtn.disabled = currentPage >= pdfDocument.numPages;
}

function updateSecurityBanner() {
    let bannerText = '🔒 Document sécurisé';

    if (permissions.isReadOnly) {
        bannerText += ' - Lecture seule';
    }

    if (!permissions.allowCopy) {
        bannerText += ' - Copie interdite';
    }

    if (!permissions.allowPrinting) {
        bannerText += ' - Impression interdite';
    }

    elements.securityBannerText.textContent = bannerText;
}

async function changePage(delta) {
    const newPage = currentPage + delta;

    if (newPage < 1 || newPage > pdfDocument.numPages) {
        return;
    }

    currentPage = newPage;
    await renderPage(currentPage);
}

async function changeZoom(delta) {
    const newZoom = Math.max(0.5, Math.min(3.0, currentZoom + delta));

    if (newZoom === currentZoom) {
        return;
    }

    currentZoom = newZoom;
    elements.zoomLevel.textContent = `${Math.round(currentZoom * 100)}%`;

    await renderPage(currentPage);
}

function handlePrint() {
    if (!permissions.allowPrinting) {
        alert('❌ L\'impression n\'est pas autorisée pour ce document');
        return;
    }

    window.print();
}

function handleDownload() {
    alert('❌ Le téléchargement est désactivé pour des raisons de sécurité');
}

function closePdfViewer() {
    showSection('upload');
    resetViewer();
}

// ============================================
// MESURES DE SÉCURITÉ
// ============================================

function applySecurityMeasures() {
    // Désactiver le clic droit
    document.addEventListener('contextmenu', (e) => {
        if (elements.viewerSection.style.display !== 'none') {
            e.preventDefault();
            return false;
        }
    });

    // Désactiver les raccourcis clavier de copie
    document.addEventListener('keydown', (e) => {
        if (elements.viewerSection.style.display !== 'none') {
            // Ctrl+C, Ctrl+P, Ctrl+S, etc.
            if ((e.ctrlKey || e.metaKey) && ['c', 'p', 's', 'a'].includes(e.key.toLowerCase())) {
                if (!permissions.allowCopy && e.key.toLowerCase() === 'c') {
                    e.preventDefault();
                    alert('❌ La copie n\'est pas autorisée');
                    return false;
                }
                if (!permissions.allowPrinting && e.key.toLowerCase() === 'p') {
                    e.preventDefault();
                    alert('❌ L\'impression n\'est pas autorisée');
                    return false;
                }
            }
        }
    });

    // Détecter les tentatives de screenshot (limité)
    document.addEventListener('keyup', (e) => {
        // PrintScreen
        if (e.key === 'PrintScreen') {
            console.warn('⚠️ Tentative de capture d\'écran détectée');
            // On ne peut pas bloquer, mais on peut logger
        }
    });
}

// ============================================
// UTILITAIRES
// ============================================

function showSection(section) {
    elements.uploadSection.style.display = section === 'upload' ? 'block' : 'none';
    elements.validationSection.style.display = section === 'validation' ? 'block' : 'none';
    elements.resultsSection.style.display = section === 'results' ? 'block' : 'none';
    elements.viewerSection.style.display = section === 'viewer' ? 'block' : 'none';
}

function resetViewer() {
    selectedFile = null;
    pdfDocument = null;
    currentPage = 1;
    currentZoom = 1.0;
    validationResult = null;
    permissions = null;

    elements.fileInput.value = '';
    elements.expectedHash.value = '';
    elements.userPassword.value = '';
    elements.fileInfo.style.display = 'none';
    elements.validateBtn.disabled = true;

    showSection('upload');
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

// Export pour utilisation externe (si nécessaire)
window.SecureDocumentPdf = {
    reset: resetViewer,
    getValidationResult: () => validationResult,
    getPermissions: () => permissions
};