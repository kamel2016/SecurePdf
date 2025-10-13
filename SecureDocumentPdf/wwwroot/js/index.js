// INDEX PAGE SCRIPTS

document.addEventListener('DOMContentLoaded', function () {
    setupFileHandling();
    setupFormEvents();
});

// ====== FILE HANDLING ======
function setupFileHandling() {
    const dropZone = document.getElementById('dropZone');
    const fileInput = document.getElementById('fileInput');
    const fileInfo = document.getElementById('fileInfo');

    if (!dropZone || !fileInput) return;

    // Prevent default drag behaviors
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, preventDefaults);
        document.body.addEventListener(eventName, preventDefaults);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    // Highlight drop zone when item is dragged over it
    ['dragenter', 'dragover'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => {
            dropZone.classList.add('dragover');
        });
    });

    ['dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => {
            dropZone.classList.remove('dragover');
        });
    });

    // Handle dropped files
    dropZone.addEventListener('drop', handleDrop);
    fileInput.addEventListener('change', handleFileSelect);

    function handleDrop(e) {
        const dt = e.dataTransfer;
        const files = dt.files;
        if (files.length > 0) {
            fileInput.files = files;
            displayFileInfo(files[0]);
        }
    }

    function handleFileSelect(e) {
        const files = e.target.files;
        if (files.length > 0) {
            displayFileInfo(files[0]);
        }
    }

    function displayFileInfo(file) {
        const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();

        document.getElementById('fileName').textContent = file.name;
        document.getElementById('fileSize').textContent = formatFileSize(file.size);
        document.getElementById('fileIcon').innerHTML = getFileIcon(ext);

        fileInfo.classList.add('show');
        dropZone.style.display = 'none';
    }
}

window.resetFileSelection = function () {
    const fileInput = document.getElementById('fileInput');
    const fileInfo = document.getElementById('fileInfo');
    const dropZone = document.getElementById('dropZone');

    fileInput.value = '';
    fileInfo.classList.remove('show');
    dropZone.style.display = 'block';
};

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function getFileIcon(ext) {
    const icons = {
        '.pdf': '<i class="fas fa-file-pdf"></i>',
        '.doc': '<i class="fas fa-file-word"></i>',
        '.docx': '<i class="fas fa-file-word"></i>',
        '.xls': '<i class="fas fa-file-excel"></i>',
        '.xlsx': '<i class="fas fa-file-excel"></i>',
        '.ppt': '<i class="fas fa-file-powerpoint"></i>',
        '.pptx': '<i class="fas fa-file-powerpoint"></i>',
        '.jpg': '<i class="fas fa-image"></i>',
        '.jpeg': '<i class="fas fa-image"></i>',
        '.png': '<i class="fas fa-image"></i>',
        '.bmp': '<i class="fas fa-image"></i>',
        '.gif': '<i class="fas fa-image"></i>',
        '.svg': '<i class="fas fa-image"></i>',
        '.tiff': '<i class="fas fa-image"></i>',
        '.txt': '<i class="fas fa-file-text"></i>',
        '.csv': '<i class="fas fa-table"></i>',
        '.json': '<i class="fas fa-code"></i>',
        '.xml': '<i class="fas fa-code"></i>',
        '.html': '<i class="fas fa-code"></i>',
        '.htm': '<i class="fas fa-code"></i>',
        '.md': '<i class="fas fa-file-alt"></i>',
        '.rtf': '<i class="fas fa-file-text"></i>',
        '.eml': '<i class="fas fa-envelope"></i>',
        '.odt': '<i class="fas fa-file-word"></i>',
        '.ods': '<i class="fas fa-file-excel"></i>',
        '.odp': '<i class="fas fa-file-powerpoint"></i>'
    };
    return icons[ext] || '<i class="fas fa-file"></i>';
}

// ====== FORM EVENTS ======
function setupFormEvents() {
    const multiSigCheck = document.getElementById('multiSigCheck');
    const signersGroup = document.getElementById('signersGroup');
    const uploadForm = document.getElementById('uploadForm');

    // Multi-signature toggle
    if (multiSigCheck) {
        multiSigCheck.addEventListener('change', function () {
            if (signersGroup) {
                signersGroup.style.display = this.checked ? 'block' : 'none';
            }
        });
    }

    // Form validation
    if (uploadForm) {
        uploadForm.addEventListener('submit', function (e) {
            const userNameInput = uploadForm.querySelector('input[name="UserName"]');
            const fileInput = document.getElementById('fileInput');

            if (!userNameInput || !userNameInput.value.trim()) {
                e.preventDefault();
                console.error('Nom d\'utilisateur vide');
                alert('Veuillez entrer votre nom d\'utilisateur');
                return false;
            }

            if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
                e.preventDefault();
                console.error('Aucun fichier sélectionné');
                alert('Veuillez sélectionner un fichier');
                return false;
            }

            console.log('Formulaire valide, soumission...');
            // Le formulaire se soumet normalement
        });
    }
}

// ====== ADVANCED OPTIONS ======
window.toggleAdvanced = function () {
    const content = document.getElementById('advancedContent');
    const icon = document.getElementById('advancedToggleIcon');

    if (!content || !icon) return;

    content.classList.toggle('show');
    const isOpen = content.classList.contains('show');
    icon.style.transform = isOpen ? 'rotate(180deg)' : 'rotate(0deg)';
    icon.style.transition = '0.3s ease';
};

// ====== ACCORDION ======
window.toggleAccordion = function (header) {
    if (!header) return;

    const content = header.nextElementSibling;
    if (!content) return;

    const isOpen = content.classList.contains('show');

    // Close all accordions
    document.querySelectorAll('.accordion-content.show').forEach(el => {
        el.classList.remove('show');
    });

    // Open clicked accordion if it was closed
    if (!isOpen) {
        content.classList.add('show');
    }
};