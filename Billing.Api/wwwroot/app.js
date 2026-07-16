const form = document.getElementById("order-form");
const result = document.getElementById("result");

form.addEventListener("submit", async (event) => {
    event.preventDefault();
    clearResult();
    setLoading();

    const payload = {
        orderNumber: document.getElementById("orderNumber").value.trim(),
        userId: document.getElementById("userId").value.trim(),
        amount: Number(document.getElementById("amount").value),
        paymentGateway: document.getElementById("paymentGateway").value,
        description: document.getElementById("description").value.trim() || null
    };

    try {
        const response = await fetch("/api/orders", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(payload)
        });

        const contentType = response.headers.get("content-type") || "";
        const body = contentType.includes("json")
            ? await response.json()
            : null;

        if (response.ok) {
            renderSuccess(response.status, body);
            return;
        }

        renderProblem(response.status, body);
    } catch (error) {
        renderNetworkError(error);
    }
});

function clearResult() {
    result.innerHTML = "";
}

function setLoading() {
    result.innerHTML = "<div class='row'>Submitting...</div>";
}

function renderSuccess(status, body) {
    const processedAt = body?.timestamp
        ? new Date(body.timestamp).toLocaleString()
        : "-";
    const isReplay = status === 200;

    result.innerHTML = `
        <div class="row"><strong>HTTP Status:</strong> ${escapeHtml(String(status))}</div>
        <div class="row"><strong>Order Number:</strong> ${escapeHtml(body?.orderNumber ?? "-")}</div>
        <div class="row"><strong>Amount:</strong> ${escapeHtml(String(body?.amount ?? "-"))}</div>
        <div class="row"><strong>Confirmation Number:</strong> ${escapeHtml(body?.confirmationNumber ?? "-")}</div>
        <div class="row"><strong>Processed At:</strong> ${escapeHtml(processedAt)}</div>
        <div class="row"><strong>Existing Order Replay:</strong> ${isReplay ? "Yes" : "No"}</div>
    `;
}

function renderProblem(status, body) {
    const title = body?.title ?? "Request failed";
    const detail = body?.detail ?? "No details provided.";
    const errors = body?.errors;

    let html = `
        <div class="row"><strong>HTTP Status:</strong> ${escapeHtml(String(status))}</div>
        <div class="row"><strong>Title:</strong> ${escapeHtml(title)}</div>
        <div class="row"><strong>Detail:</strong> ${escapeHtml(detail)}</div>
    `;

    if (errors && typeof errors === "object") {
        html += "<div class='field-group'><div class='field-title'>Validation Errors:</div>";
        for (const [field, messages] of Object.entries(errors)) {
            const joined = Array.isArray(messages) ? messages.join(", ") : String(messages);
            html += `<div class="row"><strong>${escapeHtml(field)}:</strong> ${escapeHtml(joined)}</div>`;
        }

        html += "</div>";
    }

    result.innerHTML = html;
}

function renderNetworkError(error) {
    const message = error instanceof Error ? error.message : String(error);
    result.innerHTML = `
        <div class="row"><strong>HTTP Status:</strong> -</div>
        <div class="row"><strong>Title:</strong> Network error</div>
        <div class="row"><strong>Detail:</strong> ${escapeHtml(message)}</div>
    `;
}

function escapeHtml(value) {
    return value
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#39;");
}
