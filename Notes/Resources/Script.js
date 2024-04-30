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
    a.classList.add("external");
    a.addEventListener("click", LinkControl);
  }
}

function TaskListCheckBoxClick(event) {
  let jj = {
    "cmd": "taskUpdate",
    "id": event.target.id,
    "text": event.target.parentElement.innerText,
    "checked": event.target.checked
  };
  window.chrome.webview.postMessage(jj);
}

function uuidv4() {
  return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
    (+c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> +c / 4).toString(16)
  );
}

for (let list of document.getElementsByClassName('contains-task-list')) {
  for (let input of list.getElementsByTagName("input")) {
    if (input.type == 'checkbox') {
      input.id = "task-cb-" + uuidv4();
      input.disabled = null;
      input.addEventListener('change', TaskListCheckBoxClick);
    }
  }
}

