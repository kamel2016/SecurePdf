// Script JavaScript pour l'application PDF Security
// Gestion de l'interface utilisateur, drag & drop, et communication AJAX

$(document).ready(function () {
    initializeFileUpload();
    initializeFormValidation();
});

/**
 * Initialise la fonctionnalité de drag & drop pour les fichiers
 */
function initializeFileUpload() {
    const $fileDropArea = $('#fileDropArea');
    const $fileInput = $('#pdfFile');
    const $form = $('#uploadForm');

    // Gestion du drag & drop
    $fileDropArea.on('dragover dragenter', function (e) {
        e.preventDefault();
        e.stopPropagation();
        $(this).addClass('dragover');
    });

    $fileDropArea.on('dragleave dragend', function (e) {
        e.preventDefault();
        e.stopPropagation();
        $(this).removeClass('dragover');
    });

    $fileDropArea.on('drop', function (e) {
        e.preventDefault();
        e.stopPropagation();
        $(this).removeClass('dragover');

        const files = e.originalEvent.dataTransfer.files;
        if (files.length > 0) {
            handleFileSelection(files[0]);
        }
    });

    // Gestion du clic sur la zone de drop
    $fileDropArea.on('click', function (e) {
        if (e.target === this || $(e.target).hasClass('file-drop-icon') || $(e.target).hasClass('file-drop-text')) {
            $fileInput.click();
        }
    });

    // Gestion de la sélection de fichier via l'input
    $fileInput.on('change', function (e) {
        if (this.files && this.files[0]) {
            handleFileSelection(this.files[0]);
        }
    });

    // Soumission du formulaire
    $form.on('submit', function (e) {
        e.preventDefault();
        processPdfUpload();
    });
}

/**
 * Gère la sélection d'un fichier (validation et affichage)
 */
function handleFileSelection(file) {
    const $fileDropArea = $('#fileDropArea');

    // Validation du type de fichier
    if (file.type !== 'application/pdf') {
        showError('Seuls les fichiers PDF sont acceptés.');
        return;
    }

    // Validation de la taille (50MB max)
    const maxSize = 50 * 1024 * 1024; // 50MB en bytes
    if (file.size > maxSize) {
        showError('Le fichier est trop volumineux. Taille maximale : 50MB.');
        return;
    }

    // Mise à jour de l'interface
    $fileDropArea.addClass('file-selected');

    // Création d'un nouvel élément pour afficher les infos du fichier
    const fileInfo = `
        <div class="file-selected-info">
            <i class="fas fa-file-pdf text-success me-2"></i>
            <strong>${file.name}</strong><br>
            <small>Taille: ${formatFileSize(file.size)} • Type: PDF</small>
        </div>
    `;

    // Remplacement du contenu de la zone de drop
    $fileDropArea.html(`
        <div class="file-drop-icon">
            <i class="fas fa-check-circle fa-3x text-success mb-2"></i>
        </div>
        <div class="file-drop-text text-success">
            <strong>Fichier sélectionné</strong>
        </div>
        ${fileInfo}
        <input type="file" class="file-input" id="pdfFile" name="pdfFile" accept=".pdf,application/pdf" required>
    `);

    // Réassignation de l'événement pour le nouvel input
    const $newFileInput = $('#pdfFile');
    $newFileInput[0].files = createFileList(file);

    $newFileInput.on('change', function (e) {
        if (this.files && this.files[0]) {
            handleFileSelection(this.files[0]);
        }
    });

    // Suppression des messages d'erreur précédents
    clearErrors();
}

/**
 * Crée un FileList à partir d'un fichier (pour compatibilité avec les navigateurs)
 */
function createFileList(file) {
    const dataTransfer = new DataTransfer();
    dataTransfer.items.add(file);
    return dataTransfer.files;
}

/**
 * Formate la taille d'un fichier en unités lisibles
 */
function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

/**
 * Initialise la validation du formulaire
 */
function initializeFormValidation() {
    const $form = $('#uploadForm');

    // Validation en temps réel
    $form.find('input[required]').on('blur', function () {
        validateField($(this));
    });
}

/**
 * Valide un champ individuel
 */
function validateField($field) {
    const isValid = $field[0].checkValidity();

    if (isValid) {
        $field.removeClass('is-invalid').addClass('is-valid');
    } else {
        $field.removeClass('is-valid').addClass('is-invalid');
    }

    return isValid;
}

/**
 * Traite l'upload et la sécurisation du PDF
 */
function processPdfUpload() {
    const $form = $('#uploadForm');
    const $submitBtn = $('#submitBtn');
    const formData = new FormData($form[0]);

    // Validation du formulaire
    let isValid = true;
    $form.find('input[required]').each(function () {
        if (!validateField($(this))) {
            isValid = false;
        }
    });

    if (!isValid) {
        $form.addClass('was-validated');
        showError('Veuillez corriger les erreurs du formulaire.');
        return;
    }

    // Désactivation du bouton et affichage du statut
    $submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-2"></i>Traitement en cours...');

    showProcessingStatus();
    startProgressSimulation();

    // Envoi de la requête AJAX
    $.ajax({
        url: '/Home/UploadPdf',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        timeout: 300000, // 5 minutes timeout
        success: function (response) {
            handleUploadResponse(response);
        },
        error: function (xhr, status, error) {
            handleUploadError(xhr, status, error);
        },
        complete: function () {
            $submitBtn.prop('disabled', false).html('<i class="fas fa-shield-alt me-2"></i>Sécuriser mon PDF');
        }
    });
}

/**
 * Affiche la zone de statut de traitement
 */
function showProcessingStatus() {
    const $statusArea = $('#statusArea');
    const $resultArea = $('#resultArea');

    $resultArea.hide();
    $statusArea.show();

    updateStatusMessage('Initialisation du traitement de sécurisation...');
    $('#progressBar').css('width', '10%');
}

/**
 * Simule la progression du traitement
 */
function startProgressSimulation() {
    const steps = [
        { percent: 20, message: 'Validation et analyse du PDF...' },
        { percent: 35, message: 'Nettoyage des métadonnées...' },
        { percent: 50, message: 'Application du watermark...' },
        { percent: 65, message: 'Signature numérique PAdES...' },
        { percent: 80, message: 'Génération de l\'horodatage...' },
        { percent: 95, message: 'Finalisation et génération du fichier de preuve...' }
    ];

    let currentStep = 0;

    const progressInterval = setInterval(() => {
        if (currentStep < steps.length) {
            const step = steps[currentStep];
            $('#progressBar').css('width', step.percent + '%');
            updateStatusMessage(step.message);
            currentStep++;
        } else {
            clearInterval(progressInterval);
        }
    }, 2000);

    // Stockage de l'interval pour pouvoir l'arrêter
    window.progressInterval = progressInterval;
}

/**
 * Met à jour le message de statut
 */
function updateStatusMessage(message) {
    $('#statusMessage').html(`
        <div class="spinner-border spinner-border-sm text-primary me-2" role="status">
            <span class="visually-hidden">Traitement en cours...</span>
        </div>
        ${message}
    `);
}

/**
 * Gère la réponse de l'upload
 */
function handleUploadResponse(response) {
    // Arrêt de la simulation de progression
    if (window.progressInterval) {
        clearInterval(window.progressInterval);
    }

    $('#progressBar').css('width', '100%');

    if (response.success) {
        showSuccessResult(response);
    } else {
        showErrorResult(response);
    }
}

/**
 * Affiche le résultat en cas de succès
 */
function showSuccessResult(response) {
    const $statusArea = $('#statusArea');
    const $resultArea = $('#resultArea');
    const $successResult = $('#successResult');
    const $errorResult = $('#errorResult');

    // Masquage de la zone de statut et affichage du résultat
    $statusArea.hide();
    $errorResult.hide();

    // Configuration des liens de téléchargement
    $('#downloadPdfBtn').attr('href', response.securedPdfUrl);
    $('#downloadProofBtn').attr('href', response.proofFileUrl);

    // Affichage des hash
    $('#originalHashDisplay').text(response.originalHash || 'Non disponible');
    $('#processedHashDisplay').text(response.processedHash || 'Non disponible');
    $('#processedAtDisplay').text(response.processedAt || 'Non disponible');

    // Affichage des étapes complétées
    if (response.steps && response.steps.length > 0) {
        const stepsHtml = response.steps.map(step =>
            `<div class="processing-step completed">
                <i class="fas fa-check-circle text-success me-2"></i>${step}
            </div>`
        ).join('');
        $('#completedSteps').html(stepsHtml);
    }

    $successResult.show();
    $resultArea.show();

    // Animation d'entrée
    $successResult.addClass('result-card-enter');

    // Scroll vers les résultats
    $('html, body').animate({
        scrollTop: $resultArea.offset().top - 20
    }, 800);
}

/**
 * Affiche le résultat en cas d'erreur
 */
function showErrorResult(response) {
    const $statusArea = $('#statusArea');
    const $resultArea = $('#resultArea');
    const $successResult = $('#successResult');
    const $errorResult = $('#errorResult');

    // Masquage de la zone de statut et affichage de l'erreur
    $statusArea.hide();
    $successResult.hide();

    $('#errorMessage').text(response.message || 'Une erreur inconnue s\'est produite.');

    if (response.errorDetails) {
        $('#errorDetails').html(`<strong>Détails:</strong> ${response.errorDetails}`);
    }

    $errorResult.show();
    $resultArea.show();

    // Animation d'entrée
    $errorResult.addClass('result-card-enter');

    // Scroll vers les résultats
    $('html, body').animate({
        scrollTop: $resultArea.offset().top - 20
    }, 800);
}

/**
 * Gère les erreurs d'upload
 */
function handleUploadError(xhr, status, error) {
    // Arrêt de la simulation de progression
    if (window.progressInterval) {
        clearInterval(window.progressInterval);
    }

    let errorMessage = 'Erreur de communication avec le serveur.';
    let errorDetails = '';

    if (status === 'timeout') {
        errorMessage = 'Timeout: Le traitement a pris trop de temps.';
        errorDetails = 'Veuillez réessayer avec un fichier plus petit ou réessayer plus tard.';
    } else if (xhr.status === 413) {
        errorMessage = 'Fichier trop volumineux.';
        errorDetails = 'La taille maximale autorisée est de 50MB.';
    } else if (xhr.status === 0) {
        errorMessage = 'Erreur de connexion réseau.';
        errorDetails = 'Vérifiez votre connexion internet et réessayez.';
    } else {
        errorDetails = `Status: ${xhr.status}, Error: ${error}`;
    }

    showErrorResult({
        success: false,
        message: errorMessage,
        errorDetails: errorDetails
    });
}

/**
 * Affiche un message d'erreur temporaire
 */
function showError(message) {
    // Création d'une alerte temporaire
    const alertHtml = `
        <div class="alert alert-danger alert-dismissible fade show" role="alert">
            <i class="fas fa-exclamation-triangle me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;

    // Insertion de l'alerte avant le formulaire
    $('#uploadForm').before(alertHtml);

    // Suppression automatique après 5 secondes
    setTimeout(() => {
        $('.alert').alert('close');
    }, 5000);
}

/**
 * Efface les messages d'erreur
 */
function clearErrors() {
    $('.alert-danger').remove();
    $('.is-invalid').removeClass('is-invalid');
    $('.is-valid').removeClass('is-valid');
}

/**
 * Remet le formulaire à zéro
 */
function resetForm() {
    const $form = $('#uploadForm');
    const $fileDropArea = $('#fileDropArea');
    const $statusArea = $('#statusArea');
    const $resultArea = $('#resultArea');

    // Reset du formulaire
    $form[0].reset();
    $form.removeClass('was-validated');

    // Reset de la zone de drop
    $fileDropArea.removeClass('file-selected dragover').html(`
        <div class="file-drop-icon">
            <i class="fas fa-cloud-upload-alt fa-3x text-primary mb-2"></i>
        </div>
        <div class="file-drop-text">
            <strong>Glissez-déposez votre PDF ici</strong><br>
            ou <span class="text-primary">cliquez pour parcourir</span>
        </div>
        <input type="file" class="file-input" id="pdfFile" name="pdfFile" accept=".pdf,application/pdf" required>
    `);

    // Masquage des zones de résultat
    $statusArea.hide();
    $resultArea.hide();

    // Suppression des messages d'erreur
    clearErrors();

    // Réinitialisation des événements sur le nouveau file input
    const $newFileInput = $('#pdfFile');
    $newFileInput.on('change', function (e) {
        if (this.files && this.files[0]) {
            handleFileSelection(this.files[0]);
        }
    });

    // Scroll vers le haut
    $('html, body').animate({
        scrollTop: 0
    }, 800);
}

/**
 * Fonctions utilitaires pour l'accessibilité
 */

// Gestion du focus pour l'accessibilité
$(document).on('keydown', function (e) {
    // Échap pour fermer les alertes
    if (e.key === 'Escape') {
        $('.alert .btn-close').click();
    }
});

// Amélioration de l'accessibilité pour la zone de drag & drop
$('#fileDropArea').attr({
    'role': 'button',
    'tabindex': '0',
    'aria-label': 'Zone de téléchargement de fichier PDF. Cliquez ou glissez-déposez un fichier.'
});

// Navigation au clavier pour la zone de drop
$(document).on('keydown', '#fileDropArea', function (e) {
    if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        $('#pdfFile').click();
    }
});

/**
 * Gestion des téléchargements avec feedback utilisateur
 */
$(document).on('click', '#downloadPdfBtn, #downloadProofBtn', function (e) {
    const $btn = $(this);
    const originalText = $btn.html();

    // Feedback visuel pendant le téléchargement
    $btn.html('<i class="fas fa-spinner fa-spin me-1"></i>Téléchargement...');

    // Restauration du texte original après un délai
    setTimeout(() => {
        $btn.html(originalText);
    }, 2000);
});

/**
 * Gestion de la responsivité et des événements de redimensionnement
 */
$(window).on('resize', function () {
    // Ajustement de la hauteur des éléments si nécessaire
    adjustLayoutForMobile();
});

function adjustLayoutForMobile() {
    const isMobile = $(window).width() < 768;

    if (isMobile) {
        // Ajustements pour mobile
        $('.file-drop-area').css('min-height', '120px');
    } else {
        // Ajustements pour desktop
        $('.file-drop-area').css('min-height', '150px');
    }
}

// Initialisation des ajustements responsive
$(document).ready(function () {
    adjustLayoutForMobile();
});

/**
 * Fonctions utilitaires supplémentaires
 */

// Copie du hash dans le presse-papier
$(document).on('click', '.text-monospace', function () {
    const text = $(this).text();
    if (navigator.clipboard) {
        navigator.clipboard.writeText(text).then(() => {
            // Feedback visuel
            const $this = $(this);
            const originalBg = $this.css('background-color');
            $this.css('background-color', '#dcfce7');
            setTimeout(() => {
                $this.css('background-color', originalBg);
            }, 1000);

            // Notification temporaire
            showTemporaryNotification('Hash copié dans le presse-papier !');
        }).catch(() => {
            // Fallback pour anciens navigateurs
            showTemporaryNotification('Impossible de copier automatiquement');
        });
    }
});

/**
 * Affiche une notification temporaire
 */
function showTemporaryNotification(message) {
    const notificationHtml = `
        <div class="alert alert-success alert-dismissible fade show position-fixed" 
             style="top: 20px; right: 20px; z-index: 9999; min-width: 300px;">
            <i class="fas fa-check-circle me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;

    $('body').append(notificationHtml);

    // Suppression automatique après 3 secondes
    setTimeout(() => {
        $('.alert-success').alert('close');
    }, 3000);
}

// Validation en temps réel améliorée
$(document).on('input', '#userName', function () {
    const $this = $(this);
    const value = $this.val().trim();

    if (value.length > 0) {
        $this.removeClass('is-invalid').addClass('is-valid');
    } else {
        $this.removeClass('is-valid').addClass('is-invalid');
    }
});

// Prévention des soumissions multiples
let isSubmitting = false;

$(document).on('submit', '#uploadForm', function (e) {
    if (isSubmitting) {
        e.preventDefault();
        return false;
    }
    isSubmitting = true;

    // Réactivation après 5 secondes (sécurité)
    setTimeout(() => {
        isSubmitting = false;
    }, 5000);
});

// Réactivation après traitement complet
$(document).ajaxComplete(function () {
    isSubmitting = false;
});

/**
 * Gestion des erreurs JavaScript globales
 */
window.onerror = function (msg, url, lineNo, columnNo, error) {
    console.error('Erreur JavaScript:', {
        message: msg,
        source: url,
        line: lineNo,
        column: columnNo,
        error: error
    });

    showError('Une erreur inattendue s\'est produite. Veuillez actualiser la page.');
    return false;
};

/**
 * Nettoyage automatique des ressources
 */
$(window).on('beforeunload', function () {
    // Nettoyage des intervalles
    if (window.progressInterval) {
        clearInterval(window.progressInterval);
    }
});