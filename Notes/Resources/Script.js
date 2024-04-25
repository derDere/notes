function UpdateDates() {
    let dates = document.getElementsByClassName('date-time-field');
    for (let field of dates) {
        let id = field.id;
        let toUtc = field.dataset.utc == "1";
        let format = field.dataset.format;
        let jj = {
            "cmd": "dateUpdate",
            "id": id,
            "utc": toUtc,
            "format": format,
        };
        window.chrome.webview.postMessage(jj);
    }
}

function UpdateDateEle(jj) {
    let ele = document.getElementById(jj.id);
    ele.innerText = jj.value;
}

function Webview_Chrome_Message(event) {
    let jj = event.data;
    switch (jj.cmd) {
        case 'updateDate':
            UpdateDateEle(jj);
            break;
    }
}

window.chrome.webview.addEventListener('message', Webview_Chrome_Message);

function Doc_MouseDown(event) {
    if (event.button === 0 && event.ctrlKey) {
        let jj = {
            "cmd": "ctrlClick"
        };
        window.chrome.webview.postMessage(jj);
    }
}

document.addEventListener('mousedown', Doc_MouseDown);

setInterval(UpdateDates, 1000);

UpdateDates();

function LinkControl(event) {
    let jj = {
        "cmd": "openLink",
        "url": event.target.href,
        "target": event.target.target,
    };
    window.chrome.webview.postMessage(jj);
    event.preventDefault();
    return false;
}

for (let a of document.getElementsByTagName("a")) {
    let url = a.href.replace("about:blank", '');
    if (url[0] != "#") {
        a.addEventListener("click", LinkControl);
    }
}