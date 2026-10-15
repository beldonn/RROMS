// General site behaviour shared across pages.
(function () {
    'use strict';

    // Auto-dismiss success alerts after a few seconds.
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.alert-auto-dismiss').forEach(function (el) {
            setTimeout(function () {
                var alert = bootstrap.Alert.getOrCreateInstance(el);
                alert.close();
            }, 5000);
        });

        // Enable Bootstrap tooltips where present.
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            new bootstrap.Tooltip(el);
        });

        // Generic modal pre-fill: any button with data-modal-target + data-fields="id,name,..."
        // copies its data-* values into the matching #<modalId>_<field> input/select/textarea.
        document.querySelectorAll('[data-edit-modal]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var modalId = btn.getAttribute('data-edit-modal');
                var fields = (btn.getAttribute('data-fields') || '').split(',').filter(Boolean);
                fields.forEach(function (field) {
                    var target = document.getElementById(modalId + '_' + field);
                    var value = btn.getAttribute('data-' + field);
                    if (target && value !== null) {
                        if (target.type === 'checkbox') {
                            target.checked = value === 'true';
                        } else {
                            target.value = value;
                        }
                    }
                });
            });
        });
    });
})();
