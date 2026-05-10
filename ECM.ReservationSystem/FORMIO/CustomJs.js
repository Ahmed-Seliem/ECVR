(function () {
    const originalAjax = $.ajax;
    const targetUrl = "/Document/SaveAndSend";
    const phoneRegex = /^(010|011|012|015)[0-9]{8}$/;

    window.reservationHoldSucceeded = false;
    window.reservationHoldKey = "";
    window.reservationAutoSending = false;

    $.ajax = function (options) {
        if (
            !options ||
            !options.url ||
            !options.url.includes(targetUrl) ||
            options.skipReservationValidation === true
        ) {
            return originalAjax.apply($, arguments);
        }

        const dataObj = parseRequestPayload(options.data);
		
		const currentDocTypeId = Number(dataObj.DocumentTypeId);

		const allowedDocTypes = [
			Number(window.EmployeeDocTypeBaseID),
			Number(window.PensionDocTypeBaseID),
			Number(window.ManagerDocTypeBaseID)
		];

		if (!allowedDocTypes.includes(currentDocTypeId)) {
			return originalAjax.apply($, arguments);
		}

        const deferred = $.Deferred();
        const currentPayload = buildReservationPayload(dataObj);
        const currentKey = buildHoldKey(currentPayload);

        if (!window.reservationHoldSucceeded || !window.reservationHoldKey || window.reservationHoldKey !== currentKey) {
            showHoldStatus("برجاء الضغط علي زر تاكيد حجز الوحدة  وتاكيد الطلب", "error");
            Common.alertMsg("برجاء الضغط علي زر تاكيد حجز الوحدة  وتاكيد الطلب");
            disableSendButton();
            deferred.reject();
            return deferred.promise();
        }

        const saveOptions = $.extend(true, {}, options, {
            skipReservationValidation: true
        });

        originalAjax(saveOptions)
            .done(function (data, textStatus, jqXHR) {
                if (typeof options.success === "function") {
                    options.success(data, textStatus, jqXHR);
                }

                deferred.resolve(data, textStatus, jqXHR);
            })
            .fail(function (jqXHR, textStatus, errorThrown) {
                if (typeof options.error === "function") {
                    options.error(jqXHR, textStatus, errorThrown);
                }

                window.reservationAutoSending = false;

                let msg = "فشل إرسال الطلب.";
                if (jqXHR.responseJSON && jqXHR.responseJSON.message) {
                    msg = jqXHR.responseJSON.message;
                }

                showHoldStatus(msg, "error");
                deferred.reject(jqXHR, textStatus, errorThrown);
            })
            .always(function (dataOrJqXHR, textStatus) {
                if (typeof options.complete === "function") {
                    options.complete(dataOrJqXHR, textStatus);
                }
            });

        return deferred.promise();
    };

    $(document).on("click", "#reserveUnitHoldBtn", function () {
        const payload = buildReservationPayload();

        const validationMessage = validateBeforeHold(payload);
        if (validationMessage) {
            resetHoldState();
            showHoldStatus(validationMessage, "error");
            Common.alertMsg(validationMessage);
            return;
        }

        showHoldStatus("جاري حجز الوحدة مؤقتًا...", "info");
        disableSendButton();
        $("#reserveUnitHoldBtn").prop("disabled", true);

        originalAjax({
            url: window.ReservationURL + "/api/Reservation/hold",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(payload)
        })
            .done(function () {
                window.reservationHoldSucceeded = true;
                window.reservationHoldKey = buildHoldKey(payload);

                showHoldStatus("تم حجز الوحدة مؤقتًا، جاري إرسال الطلب...", "success");
                enableSendButton();

                window.reservationAutoSending = true;
                triggerSendButton();
            })
            .fail(function (xhr) {
                resetHoldState();

                let msg = "فشل حجز الوحدة ";
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    msg = xhr.responseJSON.message;
                }

                showHoldStatus(msg, "error");
                $("#reserveUnitHoldBtn").prop("disabled", false);
            });
    });

    $(document).on(
        "change",
        "[name='data[number]'], [name='data[phoneNumber]'], [name='data[propertyWithFloor]'], [name='data[weeks]'], [name='data[passengers]'], [name='data[city]']",
        function () {
            resetHoldState();
            showHoldStatus("تم تغيير بيانات الحجز. برجاء الضغط على حجز الوحدة  مرة أخرى.", "warning");
        }
    );

    $(document).ready(function () {
        disableSendButton();
        $("#reserveUnitHoldTimer").html("");
    });

    function validateBeforeHold(payload) {
        if (!payload.employeeNumber || !String(payload.employeeNumber).trim()) {
            return "برجاء إدخال رقم العامل.";
        }

        if (!payload.phoneNumber || !String(payload.phoneNumber).trim()) {
            return "برجاء إدخال رقم التليفون.";
        }

        if (!phoneRegex.test(String(payload.phoneNumber).trim())) {
            return "رقم التليفون غير صحيح.";
        }

        if (!payload.weekId) {
            return "اختر الرحلة أولًا.";
        }

        if (!payload.unitId) {
            return "اختر الوحدة أولًا.";
        }

        return "";
    }

    function buildReservationPayload(dataObj) {
        dataObj = dataObj || {};

        const passengers = Number(getFieldValue("passengers") || 0);

        return {
            employeeNumber: getFieldValue("number") || "",
            employeeName: getFieldValue("name") || "",
            phoneNumber: getFieldValue("phoneNumber") || "",
            unitId: toNumber(getFieldValue("propertyWithFloor")),
            weekId: getFieldValue("weeks") || "",
            numberOfGuests: passengers > 0 ? passengers : 1,
            isTransportationRequired: passengers > 0,
            paymentReceiptNumber: getFieldValue("paymentReceiptNumber") || getFieldValue("textField") || "",
            insuranceReceiptNumber: getFieldValue("insuranceReceiptNumber") || "",
            notes: "",
            caseSystemId: dataObj.id || window.DocumentId || null,
            workflowId: dataObj.WorkflowId ? Number(dataObj.WorkflowId) : (window.WorkflowId ? Number(window.WorkflowId) : null),
            documentId: dataObj.DocumentId ? Number(dataObj.DocumentId) : (window.DocumentId ? Number(window.DocumentId) : null),
            department: getFieldValue("department") || "",
            sector: getFieldValue("sector") || "",
            employeeDepartment: getFieldValue("employeeDepartment") || ""
        };
    }

    function getFieldValue(key) {
        const $el = $("[name='data[" + key + "]']");
        if ($el.length) {
            return $el.first().val();
        }
        return "";
    }

    function toNumber(value) {
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : 0;
    }

    function buildHoldKey(payload) {
        return [
            payload.unitId || 0,
            payload.weekId || "",
            payload.numberOfGuests || 0,
            payload.isTransportationRequired ? 1 : 0
        ].join("|");
    }

    function getSendButtons() {
        return $("button, input[type='button'], input[type='submit']").filter(function () {
            const $el = $(this);
            const text = (($el.text() || $el.val() || "") + "").trim().toLowerCase();

            return $el.hasClass("btn-wizard-nav-submit") ||
                $el.hasClass("btn-wf-send") ||
                $el.attr("data-action") === "send" ||
                text === "send" ||
                text === "إرسال";
        });
    }

    function getPrimarySendButton() {
        return getSendButtons().first();
    }

    function triggerSendButton() {
        const $sendButton = getPrimarySendButton();

        if (!$sendButton.length) {
            window.reservationAutoSending = false;
            showHoldStatus("تم الحجز المؤقت، لكن تعذر العثور على زر الإرسال.", "error");
            return;
        }

        setTimeout(function () {
            $sendButton.trigger("click");
        }, 250);
    }

    function disableSendButton() {
        getSendButtons().prop("disabled", true);
    }

    function enableSendButton() {
        getSendButtons().prop("disabled", false);
    }

    function resetHoldState() {
        window.reservationHoldSucceeded = false;
        window.reservationHoldKey = "";
        window.reservationAutoSending = false;
        disableSendButton();
        $("#reserveUnitHoldBtn").prop("disabled", false).show();
    }

    function showHoldStatus(message, type) {
        let color = "#0c5460";
        if (type === "success") color = "green";
        if (type === "error") color = "red";
        if (type === "warning") color = "#b36b00";

        $("#reserveUnitHoldStatus").html(
            "<span style='color:" + color + ";font-weight:bold;'>" + message + "</span>"
        );
    }

    function parseRequestPayload(data) {
        const obj = {};

        if (!data) return obj;

        if (typeof data === "string") {
            data.split("&").forEach(function (pair) {
                const index = pair.indexOf("=");

                const key = decodeURIComponent(index >= 0 ? pair.substring(0, index) : pair);
                const value = decodeURIComponent(
                    (index >= 0 ? pair.substring(index + 1) : "").replace(/\+/g, " ")
                );

                if (key) obj[key] = value;
            });

            return obj;
        }

        if (data instanceof FormData) {
            data.forEach(function (value, key) {
                obj[key] = value;
            });

            return obj;
        }

        return data;
    }
})();
