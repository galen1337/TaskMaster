// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
	function ensureModal() {
		let modal = document.getElementById('cardDetailsModal');
		if (modal) return modal;
		const html = `
<div class="modal fade" id="cardDetailsModal" tabindex="-1" aria-hidden="true">
	<div class="modal-dialog modal-lg modal-dialog-centered">
		<div class="modal-content">
			<div class="modal-header">
				<h5 class="modal-title">Card Details</h5>
				<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
			</div>
			<div class="modal-body">
				<div class="text-center py-5">Loading…</div>
			</div>
		</div>
	</div>
</div>`;
		document.body.insertAdjacentHTML('beforeend', html);
		return document.getElementById('cardDetailsModal');
	}

	document.addEventListener('click', async (e) => {
		const link = e.target.closest('[data-card-details]');
		if (!link) return;
		e.preventDefault();
		const cardId = link.getAttribute('data-card-details');
		const modalEl = ensureModal();
		const modal = new bootstrap.Modal(modalEl);
		modal.show();
		const body = modalEl.querySelector('.modal-body');
		body.innerHTML = '<div class="text-center py-5">Loading…</div>';
		try {
			const res = await fetch(`/Cards/Details?id=${cardId}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
			const html = await res.text();
			body.innerHTML = html;
			wireForms(body);
		} catch {
			body.innerHTML = '<div class="alert alert-danger">Failed to load card details.</div>';
		}
	});

	function toast(type, title) {
		if (window.Swal) {
			Swal.fire({ toast: true, position: 'top-end', timer: 2500, timerProgressBar: true, icon: type, title, showConfirmButton: false });
		} else {
			alert(title);
		}
	}

	// Handle TempData messages from server
	window.showTempDataMessages = function(success, error) {
		if (success && success.trim()) {
			toast('success', success);
		}
		if (error && error.trim()) {
			toast('error', error);
		}
	};

	function wireForms(scope) {
		// Edit form
		const editForm = scope.querySelector('form[action$="/Cards/Edit"]');
		if (editForm) {
			editForm.addEventListener('submit', async (e) => {
				e.preventDefault();
				const formData = new FormData(editForm);
				try {
					const res = await fetch(editForm.action, { method: 'POST', body: formData, headers: { 'X-Requested-With': 'XMLHttpRequest' } });
					if (res.ok) {
						toast('success', 'Card updated');
						location.reload();
					} else {
						const t = await res.text();
						toast('error', 'Failed to save card');
					}
				} catch { toast('error', 'Failed to save card'); }
			});
		}
		// Delete form
		const delForm = scope.querySelector('form[action$="/Cards/Delete"]');
		if (delForm) {
			delForm.addEventListener('submit', async (e) => {
				e.preventDefault();
				const confirmed = window.Swal ? await Swal.fire({ title: 'Delete this card?', icon: 'warning', showCancelButton: true, confirmButtonText: 'Delete', confirmButtonColor: '#d33' }).then(r => r.isConfirmed) : confirm('Delete this card?');
				if (!confirmed) return;
				const formData = new FormData(delForm);
				try {
					const res = await fetch(delForm.action, { method: 'POST', body: formData, headers: { 'X-Requested-With': 'XMLHttpRequest' } });
					if (res.ok) {
						toast('success', 'Card deleted');
						location.reload();
					} else { toast('error', 'Failed to delete card'); }
				} catch { toast('error', 'Failed to delete card'); }
			});
		}
		// Assign form
		const assignForm = scope.querySelector('form[action$="/Cards/Assign"]');
		if (assignForm) {
			assignForm.addEventListener('submit', async (e) => {
				e.preventDefault();
				const formData = new FormData(assignForm);
				try {
					const res = await fetch(assignForm.action, { method: 'POST', body: formData, headers: { 'X-Requested-With': 'XMLHttpRequest' } });
					if (res.ok) {
						toast('success', 'Assignment updated');
						location.reload();
					} else {
						const text = await res.text();
						toast('error', 'Failed to assign');
					}
				} catch { toast('error', 'Failed to assign'); }
			});
		}
	}
})();
