#!/usr/bin/env python3

from __future__ import annotations

import html
import json
import os
import sys
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path

from PySide6 import QtCore, QtGui, QtWidgets


WORKSPACE_ROOT = Path(__file__).resolve().parents[2]
CMAKE_BUILD_SCRIPT = WORKSPACE_ROOT / "tools" / "build" / "cmake_build.sh"
CONFIGURE_SCRIPT = WORKSPACE_ROOT / "tools" / "build" / "cmake_configure.sh"
TRACY_TOOL_SCRIPT = WORKSPACE_ROOT / "tools" / "profiling" / "tracy_tool.sh"
RENDERDOC_TOOL_SCRIPT = WORKSPACE_ROOT / "tools" / "capture" / "renderdoc_tool.sh"
BOOTSTRAP_SCRIPT = WORKSPACE_ROOT / "tools" / "bootstrap" / "workspace_bootstrap.sh"
LOG_ROOT = WORKSPACE_ROOT / "logs" / "tools"
WARNINGS_LOG = LOG_ROOT / "workspace_control_warnings.log"
ERRORS_LOG = LOG_ROOT / "workspace_control_errors.log"
STATE_PATH = LOG_ROOT / "workspace_control_status.json"
SETTINGS_PATH = LOG_ROOT / "workspace_control_settings.json"
DASHBOARD_LOG = LOG_ROOT / "workspace_control_dashboard.log"

PRESET_DETAILS = {
    "debug-linux": "Linux debug build from the active Clang preset.",
    "release-linux": "Linux release build from the active Clang preset.",
    "debug-windows": "Windows debug cross-build from Linux through the Windows Clang toolchain.",
    "release-windows": "Windows release cross-build from Linux through the Windows Clang toolchain.",
    "debug-macos": "macOS debug cross-build from Linux through the macOS Clang toolchain.",
    "release-macos": "macOS release cross-build from Linux through the macOS Clang toolchain.",
}

LINUX_DEBUG_PRESET = "debug-linux"
LINUX_RELEASE_PRESET = "release-linux"
WINDOWS_DEBUG_PRESET = "debug-windows"
WINDOWS_RELEASE_PRESET = "release-windows"
MACOS_DEBUG_PRESET = "debug-macos"
MACOS_RELEASE_PRESET = "release-macos"
PRESET_LABELS = {
    LINUX_DEBUG_PRESET: "Linux Debug",
    LINUX_RELEASE_PRESET: "Linux Release",
    WINDOWS_DEBUG_PRESET: "Windows Debug",
    WINDOWS_RELEASE_PRESET: "Windows Release",
    MACOS_DEBUG_PRESET: "macOS Debug",
    MACOS_RELEASE_PRESET: "macOS Release",
}
BUILD_PRESETS = (
    LINUX_DEBUG_PRESET,
    LINUX_RELEASE_PRESET,
    WINDOWS_DEBUG_PRESET,
    WINDOWS_RELEASE_PRESET,
    MACOS_DEBUG_PRESET,
    MACOS_RELEASE_PRESET,
)
CROSS_COMPILE_PRESETS = {
    WINDOWS_DEBUG_PRESET,
    WINDOWS_RELEASE_PRESET,
    MACOS_DEBUG_PRESET,
    MACOS_RELEASE_PRESET,
}

CRASH_DIAGNOSTICS_PRESETS = {
    LINUX_DEBUG_PRESET,
    LINUX_RELEASE_PRESET,
    WINDOWS_DEBUG_PRESET,
    WINDOWS_RELEASE_PRESET,
    MACOS_DEBUG_PRESET,
    MACOS_RELEASE_PRESET,
}
ACTIVE_PRODUCT = "octaryn-workspace"
ACTIVE_PRODUCT_LABEL = "Octaryn workspace owners"


@dataclass
class ProbeRunPlan:
    preset: str = LINUX_DEBUG_PRESET


@dataclass
class ToolSettings:
    launch_tracy_with_probe: bool = True
    launch_renderdoc_with_probe: bool = True
    capture_tracy_with_probe: bool = False
    tracy_capture_seconds: int = 10


@dataclass
class PendingCommand:
    label: str
    program: Path
    args: list[str]


@dataclass
class LogEntry:
    level: str
    text: str


WORKSPACE_BUILD_TARGET = "octaryn_all"
WORKSPACE_VALIDATE_TARGET = "octaryn_validate_all"
CLIENT_RUN_TARGET = "octaryn_client_launch_probe"


def product_build_dir(preset: str) -> Path:
    return WORKSPACE_ROOT / "build" / preset


def resolve_run_path(preset: str) -> Path | None:
    build_dir = product_build_dir(preset)
    candidates = [
        build_dir / "client" / "native" / "bin" / CLIENT_RUN_TARGET,
        build_dir / "client" / "native" / "bin" / f"{CLIENT_RUN_TARGET}.exe",
    ]
    for candidate in candidates:
        if candidate.is_file() and os.access(candidate, os.X_OK):
            return candidate
    return None


def command_invocation(program: Path, args: list[str]) -> tuple[str, list[str]]:
    if program.suffix == ".sh":
        shell = QtCore.QStandardPaths.findExecutable("bash") or "/usr/bin/bash"
        return shell, [str(program), *args]
    return str(program), list(args)


def now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def classify_line(line: str) -> str | None:
    lowered = line.lower()
    if "[error]" in lowered or " error" in lowered or lowered.startswith("error"):
        return "error"
    if "[debug]" in lowered or " debug" in lowered or lowered.startswith("debug"):
        return "debug"
    if "[warn" in lowered or " warning" in lowered or lowered.startswith("warning"):
        return "warning"
    return None


def append_text(path: Path, message: str) -> None:
    with path.open("a", encoding="utf-8") as handle:
        handle.write(f"[{now_iso()}] {message}\n")


def load_tool_settings() -> ToolSettings:
    if not SETTINGS_PATH.is_file():
        return ToolSettings()
    try:
        raw = json.loads(SETTINGS_PATH.read_text(encoding="utf-8"))
        return ToolSettings(
            launch_tracy_with_probe=bool(raw.get("launch_tracy_with_probe", True)),
            launch_renderdoc_with_probe=bool(
                raw.get("launch_renderdoc_with_probe", True)
            ),
            capture_tracy_with_probe=bool(raw.get("capture_tracy_with_probe", False)),
            tracy_capture_seconds=int(raw.get("tracy_capture_seconds", 10)),
        )
    except (OSError, ValueError, TypeError, json.JSONDecodeError):
        return ToolSettings()


def render_log_entry_html(entry: LogEntry) -> str:
    palette = {
        "info": ("#ffffff", "#000000", "#ffffff"),
        "debug": ("#dbeafe", "#000000", "#2563eb"),
        "warning": ("#fef3c7", "#000000", "#d97706"),
        "error": ("#fee2e2", "#000000", "#dc2626"),
    }
    foreground, background, accent = palette.get(entry.level, palette["info"])
    level_label = entry.level.upper()
    body = html.escape(entry.text)
    return (
        f"<div style='margin: 0 0 8px 0; padding: 10px 12px; border-radius: 8px; "
        f"background:{background}; border: 1px solid {accent}; color:{foreground};'>"
        f"<div style='white-space:pre; font-family:monospace; font-size:12px; color:{foreground};'>"
        f"<span style='color:{accent}; font-weight:700;'>[{level_label}]</span> - {body}</div>"
        "</div>"
    )


def preset_summary(preset: str) -> str:
    details = PRESET_DETAILS.get(preset, "No preset summary available.")
    crash_state = "ON" if preset in CRASH_DIAGNOSTICS_PRESETS else "OFF"
    return f"{details}\nCrash diagnostics: {crash_state}"


def probe_status_summary(preset: str) -> str:
    if preset in CROSS_COMPILE_PRESETS:
        return "Probe: cross-build"
    run_path = resolve_run_path(preset)
    if run_path is None:
        return "Probe: missing"
    return f"Probe: ready ({run_path.name})"


def build_target_for_preset(preset: str) -> str:
    return WORKSPACE_BUILD_TARGET


class EngineControlWindow(QtWidgets.QWidget):
    def __init__(self) -> None:
        super().__init__()
        self.setWindowTitle("Octaryn Workspace Control")
        self.resize(980, 680)

        LOG_ROOT.mkdir(parents=True, exist_ok=True)
        self.pending_probe_run: ProbeRunPlan | None = None
        self.pending_command_after_configure: PendingCommand | None = None
        self.pending_preset_builds: list[str] = []
        self.all_log_lines: list[LogEntry] = []
        self.pending_log_entries: list[LogEntry] = []
        self._starting_process = False
        self._stopping_process = False
        self.tool_settings = load_tool_settings()

        self.current_label = ""
        self.current_program = ""
        self.current_args: list[str] = []
        self.process = QtCore.QProcess(self)
        self.process.setProcessChannelMode(
            QtCore.QProcess.ProcessChannelMode.MergedChannels
        )
        self.process.setWorkingDirectory(str(WORKSPACE_ROOT))
        self.process.readyReadStandardOutput.connect(self._append_process_output)
        self.process.started.connect(self._handle_process_started)
        self.process.finished.connect(self._handle_process_finished)
        self.process.errorOccurred.connect(self._handle_process_error)
        self.process_start_timer = QtCore.QTimer(self)
        self.process_start_timer.setSingleShot(True)
        self.process_start_timer.timeout.connect(self._handle_process_start_timeout)
        self.process_stop_timer = QtCore.QTimer(self)
        self.process_stop_timer.setSingleShot(True)
        self.process_stop_timer.timeout.connect(self._handle_process_stop_timeout)
        self.log_flush_timer = QtCore.QTimer(self)
        self.log_flush_timer.setSingleShot(True)
        self.log_flush_timer.timeout.connect(self._flush_log_updates)

        self.product_label = QtWidgets.QLabel(ACTIVE_PRODUCT_LABEL)
        self.product_label.setWordWrap(True)

        self.preset_combo = QtWidgets.QComboBox()
        for preset in BUILD_PRESETS:
            self.preset_combo.addItem(PRESET_LABELS[preset], preset)
        self.preset_combo.setCurrentIndex(0)

        self.configure_checkbox = QtWidgets.QCheckBox("Configure before build")
        self.configure_checkbox.setChecked(True)
        self.preset_combo.currentIndexChanged.connect(self._refresh_dashboard)

        self.build_button = QtWidgets.QPushButton("Build")
        self.build_button.clicked.connect(self.build_workspace)

        self.validate_button = QtWidgets.QPushButton("Validate")
        self.validate_button.clicked.connect(self.validate_workspace)
        self.validate_button.setToolTip(
            "Run the active CMake validation aggregate for the selected preset."
        )

        self.start_button = QtWidgets.QPushButton("Start Probe")
        self.start_button.clicked.connect(self.start_probe)

        self.stop_button = QtWidgets.QPushButton("Stop")
        self.stop_button.clicked.connect(self.stop_command)

        self.status_button = QtWidgets.QPushButton("List Presets")
        self.status_button.clicked.connect(self.show_status)

        self.launch_tracy_checkbox = QtWidgets.QCheckBox("Launch Tracy with probe")
        self.launch_tracy_checkbox.setChecked(
            self.tool_settings.launch_tracy_with_probe
        )
        self.launch_tracy_checkbox.toggled.connect(lambda _checked: self._save_tool_settings())

        self.launch_renderdoc_checkbox = QtWidgets.QCheckBox(
            "Launch RenderDoc with probe"
        )
        self.launch_renderdoc_checkbox.setChecked(
            self.tool_settings.launch_renderdoc_with_probe
        )
        self.launch_renderdoc_checkbox.toggled.connect(lambda _checked: self._save_tool_settings())

        self.capture_tracy_checkbox = QtWidgets.QCheckBox("Capture Tracy after start")
        self.capture_tracy_checkbox.setChecked(
            self.tool_settings.capture_tracy_with_probe
        )
        self.capture_tracy_checkbox.toggled.connect(lambda _checked: self._save_tool_settings())

        self.tracy_seconds_spin = QtWidgets.QSpinBox()
        self.tracy_seconds_spin.setRange(1, 300)
        self.tracy_seconds_spin.setValue(self.tool_settings.tracy_capture_seconds)
        self.tracy_seconds_spin.valueChanged.connect(lambda _value: self._save_tool_settings())

        self.launch_tracy_button = QtWidgets.QPushButton("Launch Tracy")
        self.launch_tracy_button.clicked.connect(self.launch_tracy)
        self.build_debug_tools_button = QtWidgets.QPushButton("Build Tools")
        self.build_debug_tools_button.clicked.connect(self.build_debug_tools)
        self.capture_tracy_button = QtWidgets.QPushButton("Capture Tracy")
        self.capture_tracy_button.clicked.connect(self.capture_tracy)
        self.launch_renderdoc_button = QtWidgets.QPushButton("Launch RenderDoc")
        self.launch_renderdoc_button.clicked.connect(self.launch_renderdoc)
        self.capture_renderdoc_button = QtWidgets.QPushButton("Capture RenderDoc")
        self.capture_renderdoc_button.clicked.connect(self.capture_renderdoc)
        self.launch_debug_tools_button = QtWidgets.QPushButton("Launch Both")
        self.launch_debug_tools_button.clicked.connect(self.launch_debug_tools)
        self.bootstrap_button = QtWidgets.QPushButton("Bootstrap Check")
        self.bootstrap_button.clicked.connect(self.run_bootstrap_check)

        self.status_label = QtWidgets.QLabel("Idle")
        self.status_label.setWordWrap(True)

        self.output_text = QtWidgets.QTextBrowser()
        self.output_text.setReadOnly(True)
        self.output_text.setOpenExternalLinks(False)
        self.output_text.setLineWrapMode(QtWidgets.QTextEdit.LineWrapMode.NoWrap)
        self.log_filter_all_checkbox = QtWidgets.QCheckBox("All")
        self.log_filter_all_checkbox.setChecked(True)
        self.log_filter_all_checkbox.toggled.connect(self._handle_all_filter_toggled)
        self.log_filter_debug_checkbox = QtWidgets.QCheckBox("Debug")
        self.log_filter_debug_checkbox.toggled.connect(
            self._handle_level_filter_toggled
        )
        self.log_filter_warning_checkbox = QtWidgets.QCheckBox("Warnings")
        self.log_filter_warning_checkbox.toggled.connect(
            self._handle_level_filter_toggled
        )
        self.log_filter_error_checkbox = QtWidgets.QCheckBox("Errors")
        self.log_filter_error_checkbox.toggled.connect(
            self._handle_level_filter_toggled
        )

        controls_group = QtWidgets.QGroupBox("Build + Probe")
        controls_layout = QtWidgets.QGridLayout(controls_group)
        controls_layout.addWidget(QtWidgets.QLabel("Workspace"), 0, 0)
        controls_layout.addWidget(QtWidgets.QLabel("Preset"), 0, 1)
        controls_layout.addWidget(self.product_label, 1, 0)
        controls_layout.addWidget(self.preset_combo, 1, 1)
        controls_layout.addWidget(self.configure_checkbox, 1, 2)

        button_row = QtWidgets.QHBoxLayout()
        button_row.addWidget(self.build_button)
        button_row.addWidget(self.validate_button)
        button_row.addWidget(self.start_button)
        button_row.addWidget(self.stop_button)
        button_row.addWidget(self.status_button)
        button_row.addStretch(1)
        controls_layout.addLayout(button_row, 2, 0, 1, 3)
        controls_layout.addWidget(self.status_label, 3, 0, 1, 3)
        controls_layout.setColumnStretch(0, 1)
        controls_layout.setColumnStretch(1, 1)
        controls_layout.setColumnStretch(2, 1)

        automation_group = QtWidgets.QGroupBox("Workspace Actions")
        automation_layout = QtWidgets.QVBoxLayout(automation_group)
        self.build_all_button = QtWidgets.QPushButton("Build All Presets")
        self.build_all_button.clicked.connect(self.build_all_presets)
        automation_layout.addWidget(self.build_all_button)
        self.doctor_button = QtWidgets.QPushButton("List Presets")
        self.doctor_button.clicked.connect(self.run_build_doctor)
        automation_layout.addWidget(self.doctor_button)
        self.probe_run_button = QtWidgets.QPushButton("Build + Start Probe")
        self.probe_run_button.clicked.connect(self.start_probe_run)
        automation_layout.addWidget(self.probe_run_button)
        self.logs_button = QtWidgets.QPushButton("Open Logs Folder")
        self.logs_button.clicked.connect(self.open_logs_folder)
        automation_layout.addWidget(self.logs_button)

        tools_group = QtWidgets.QGroupBox("Debug Tools")
        tools_layout = QtWidgets.QGridLayout(tools_group)
        tools_layout.addWidget(self.launch_tracy_checkbox, 0, 0, 1, 2)
        tools_layout.addWidget(self.launch_renderdoc_checkbox, 1, 0, 1, 2)
        tools_layout.addWidget(self.capture_tracy_checkbox, 2, 0, 1, 2)
        tools_layout.addWidget(QtWidgets.QLabel("Tracy seconds"), 3, 0)
        tools_layout.addWidget(self.tracy_seconds_spin, 3, 1)
        tools_layout.addWidget(self.build_debug_tools_button, 4, 0, 1, 2)
        tools_layout.addWidget(self.launch_tracy_button, 5, 0)
        tools_layout.addWidget(self.capture_tracy_button, 5, 1)
        tools_layout.addWidget(self.launch_renderdoc_button, 6, 0)
        tools_layout.addWidget(self.capture_renderdoc_button, 6, 1)
        tools_layout.addWidget(self.launch_debug_tools_button, 7, 0)
        tools_layout.addWidget(self.bootstrap_button, 7, 1)
        tools_layout.setColumnStretch(0, 1)
        tools_layout.setColumnStretch(1, 1)
        console_group = QtWidgets.QGroupBox("Console")
        console_layout = QtWidgets.QVBoxLayout(console_group)
        filter_row = QtWidgets.QHBoxLayout()
        filter_row.addWidget(QtWidgets.QLabel("Show"))
        filter_row.addWidget(self.log_filter_all_checkbox)
        filter_row.addWidget(self.log_filter_debug_checkbox)
        filter_row.addWidget(self.log_filter_warning_checkbox)
        filter_row.addWidget(self.log_filter_error_checkbox)
        filter_row.addStretch(1)
        console_layout.addLayout(filter_row)
        console_layout.addWidget(self.output_text)

        left_panel = QtWidgets.QWidget()
        left_layout = QtWidgets.QVBoxLayout(left_panel)
        left_layout.setContentsMargins(0, 0, 0, 0)
        left_layout.addWidget(controls_group)
        left_layout.addWidget(automation_group)
        left_layout.addWidget(tools_group)
        left_layout.addStretch(1)

        splitter = QtWidgets.QSplitter(QtCore.Qt.Orientation.Horizontal)
        splitter.addWidget(left_panel)
        splitter.addWidget(console_group)
        splitter.setStretchFactor(0, 3)
        splitter.setStretchFactor(1, 2)

        root_layout = QtWidgets.QVBoxLayout(self)
        root_layout.addWidget(splitter)

        self._refresh_dashboard()
        self._write_state("idle")
        self._log(
            "Octaryn control ready. Build uses root tools/build helpers and the active preset-first owner layout."
        )
        self._log(f"Workspace: {WORKSPACE_ROOT}")
        self._log(f"Logs: {LOG_ROOT}")

    def selected_preset_label(self) -> str:
        return self.preset_combo.currentText()

    def selected_preset(self) -> str:
        return self.preset_combo.currentData() or LINUX_DEBUG_PRESET

    def build_workspace(self) -> None:
        preset = self.selected_preset()
        self._start_build_command(
            f"build {preset}",
            preset,
            build_target_for_preset(preset),
        )

    def validate_workspace(self) -> None:
        preset = self.selected_preset()
        self._start_build_command(
            f"validate/{preset}",
            preset,
            WORKSPACE_VALIDATE_TARGET,
        )

    def start_probe(self) -> None:
        preset = self.selected_preset()
        if preset in CROSS_COMPILE_PRESETS:
            self._log(
                f"[warning] {preset} is a cross-build and cannot be started directly on this Linux host."
            )
            self.status_label.setText("Cross-build cannot start locally")
            self._write_state("cross-runtime-not-started")
            return
        run_path = resolve_run_path(preset)
        if run_path is None:
            self._log(
                f"[error] could not find a built client launch probe for {preset}. "
                "Use Build to produce the client launch probe."
            )
            self.status_label.setText("No built probe found")
            self._write_state("missing-executable")
            return

        self._start_command(
            f"client probe {preset}",
            run_path,
            [],
        )

    def stop_command(self) -> None:
        if self.process.state() != QtCore.QProcess.ProcessState.NotRunning:
            self._log(f"[info] stopping {self.current_label}...")
            self._write_state("stopping")
            self._stopping_process = True
            self.process.terminate()
            self.process_stop_timer.start(5000)
            return

        self._log("[info] no running process to stop.")
        self._write_state("idle")

    def show_status(self) -> None:
        cmake = Path(QtCore.QStandardPaths.findExecutable("cmake") or "/usr/bin/cmake")
        self._start_command("list build presets", cmake, ["--list-presets=build"])

    def run_build_doctor(self) -> None:
        self.show_status()

    def build_all_presets(self) -> None:
        self.pending_preset_builds = list(BUILD_PRESETS)
        self._start_next_preset_build()

    def start_probe_run(self) -> None:
        self.preset_combo.setCurrentText(PRESET_LABELS[LINUX_DEBUG_PRESET])
        self.pending_probe_run = ProbeRunPlan(preset=LINUX_DEBUG_PRESET)
        if not self._start_build_command(
            f"probe run build {LINUX_DEBUG_PRESET}",
            LINUX_DEBUG_PRESET,
            build_target_for_preset(LINUX_DEBUG_PRESET),
        ):
            self.pending_probe_run = None

    def open_logs_folder(self) -> None:
        self._log(f"[info] logs folder: {LOG_ROOT}")
        opener = None
        opener_args: list[str] = []
        if sys.platform.startswith("linux"):
            opener = "xdg-open"
            opener_args = [str(LOG_ROOT)]
        elif sys.platform == "darwin":
            opener = "open"
            opener_args = [str(LOG_ROOT)]
        elif os.name == "nt":
            opener = "explorer"
            opener_args = [str(LOG_ROOT)]

        if opener is None:
            self._log("[warning] no folder opener configured for this platform.")
            return

        QtCore.QProcess.startDetached(opener, opener_args, str(WORKSPACE_ROOT))

    def build_debug_tools(self) -> None:
        preset = self.selected_preset()
        if not preset.startswith("debug-"):
            self._log(
                f"[warning] {preset} is not a debug preset. Debug tools ship with debug presets only."
            )
            return
        self._start_build_command(
            f"debug tools {preset}",
            preset,
            "octaryn_debug_tools",
        )

    def launch_tracy(self) -> None:
        self._start_detached_tool(
            "Tracy profiler",
            TRACY_TOOL_SCRIPT,
            ["--preset", self.selected_preset(), "launch-profiler"],
        )

    def capture_tracy(self) -> None:
        self._start_detached_tool(
            "Tracy capture",
            TRACY_TOOL_SCRIPT,
            [
                "--preset",
                self.selected_preset(),
                "--seconds",
                str(self.tracy_seconds_spin.value()),
                "capture",
            ],
        )

    def launch_renderdoc(self) -> None:
        self._start_detached_tool(
            "RenderDoc UI",
            RENDERDOC_TOOL_SCRIPT,
            ["--preset", self.selected_preset(), "launch"],
        )

    def capture_renderdoc(self) -> None:
        preset = self.selected_preset()
        if preset in CROSS_COMPILE_PRESETS:
            self._log(
                f"[warning] {preset} is a cross-build; RenderDoc capture must run on the target platform."
            )
            return
        run_path = resolve_run_path(preset)
        if run_path is None:
            self._log(
                f"[error] could not find a built client launch probe for RenderDoc capture on {preset}."
            )
            return
        self._start_detached_tool(
            "RenderDoc capture",
            RENDERDOC_TOOL_SCRIPT,
            ["--preset", preset, "--program", str(run_path), "capture"],
        )

    def launch_debug_tools(self) -> None:
        self.launch_tracy()
        self.launch_renderdoc()

    def run_bootstrap_check(self) -> None:
        self._start_command("bootstrap detect", BOOTSTRAP_SCRIPT, ["detect"])

    def closeEvent(self, event: QtGui.QCloseEvent) -> None:
        if self.process.state() != QtCore.QProcess.ProcessState.NotRunning:
            self.stop_command()
        super().closeEvent(event)

    def _start_build_command(self, label: str, preset: str, target: str) -> bool:
        build_command = PendingCommand(
            label=label,
            program=CMAKE_BUILD_SCRIPT,
            args=[preset, "--target", target],
        )
        if self.configure_checkbox.isChecked():
            if self._start_command(
                f"configure {preset}", CONFIGURE_SCRIPT, [preset]
            ):
                self.pending_command_after_configure = build_command
                return True
            return False
        return self._start_command(
            build_command.label, build_command.program, build_command.args
        )

    def _start_command(self, label: str, program: Path, args: list[str]) -> bool:
        if self.process.state() != QtCore.QProcess.ProcessState.NotRunning:
            self._log(
                "[info] another command is already running. Stop it before starting a new one."
            )
            self._write_state("busy")
            return False

        if not program.exists():
            self._log(f"[error] expected tool was not found: {program}")
            self.status_label.setText("Missing tool")
            self._write_state("missing-tool")
            return False

        self.current_label = label
        launch_program, launch_args = command_invocation(program, args)
        self.current_program = launch_program
        self.current_args = list(launch_args)
        self.status_label.setText(f"Running: {label}")
        self._log(f"$ {launch_program} {' '.join(launch_args)}".rstrip())
        self._write_state("running")
        self._starting_process = True
        self.process.start(launch_program, launch_args)
        self.process_start_timer.start(3000)
        self._refresh_dashboard()
        return True

    def _start_detached_tool(self, label: str, program: Path, args: list[str]) -> bool:
        if not program.exists():
            self._log(f"[error] expected debug tool was not found: {program}")
            return False
        launch_program, launch_args = command_invocation(program, args)
        self._log(f"$ {launch_program} {' '.join(launch_args)}".rstrip())
        if QtCore.QProcess.startDetached(launch_program, launch_args, str(WORKSPACE_ROOT)):
            self._log(f"[info] launched {label}; tool logs write under {LOG_ROOT}.")
            return True
        self._log(f"[error] failed to launch {label}.")
        return False

    def _handle_process_started(self) -> None:
        self._starting_process = False
        self.process_start_timer.stop()
        label = self.current_label
        if label.startswith("client probe ") or label.startswith("probe run "):
            QtCore.QTimer.singleShot(
                1200, lambda completed_label=label: self._launch_post_command_tools(completed_label)
            )

    def _handle_process_start_timeout(self) -> None:
        if (
            self._starting_process
            and self.process.state() == QtCore.QProcess.ProcessState.Starting
        ):
            self._starting_process = False
            self._log(f"[error] failed to start {self.current_label}.")
            self.status_label.setText(f"Failed: {self.current_label}")
            self._write_state("failed-to-start")
            self.process.kill()

    def _handle_process_stop_timeout(self) -> None:
        if (
            self._stopping_process
            and self.process.state() != QtCore.QProcess.ProcessState.NotRunning
        ):
            self._log(
                f"[warning] force-killing {self.current_label} after stop timeout."
            )
            self.process.kill()

    def _append_process_output(self) -> None:
        output = bytes(self.process.readAllStandardOutput()).decode(errors="replace")
        if output:
            for line in output.splitlines():
                self._log(line)

    def _handle_process_finished(
        self, exit_code: int, _exit_status: QtCore.QProcess.ExitStatus
    ) -> None:
        self.process_start_timer.stop()
        self.process_stop_timer.stop()
        self._starting_process = False
        self._stopping_process = False
        completed_label = self.current_label
        succeeded = exit_code == 0
        if exit_code == 0:
            self._log(f"[info] {self.current_label} finished successfully.")
            self.status_label.setText(f"Finished: {self.current_label}")
            self._write_state("finished", exit_code)
        else:
            self._log(f"[error] {self.current_label} exited with code {exit_code}.")
            self.status_label.setText(f"Failed: {self.current_label}")
            self._write_state("failed", exit_code)
        self.current_label = ""
        self.current_program = ""
        self.current_args = []
        if self._continue_pending_after_configure(exit_code, completed_label):
            return
        continued = self._continue_probe_run(exit_code, completed_label)
        if succeeded and not continued and self._start_next_preset_build():
            return
        if succeeded and not continued:
            self._launch_post_command_tools(completed_label)
        self._refresh_dashboard()

    def _start_next_preset_build(self) -> bool:
        if not self.pending_preset_builds:
            return False
        preset = self.pending_preset_builds.pop(0)
        return self._start_build_command(
            f"build {preset}",
            preset,
            WORKSPACE_BUILD_TARGET,
        )

    def _continue_pending_after_configure(
        self, exit_code: int, completed_label: str
    ) -> bool:
        pending_command = self.pending_command_after_configure
        if pending_command is None or not completed_label.startswith("configure "):
            return False
        self.pending_command_after_configure = None
        if exit_code != 0:
            self._log("[error] build skipped because configure failed.")
            return False
        self._start_command(
            pending_command.label, pending_command.program, pending_command.args
        )
        return True

    def _handle_process_error(self, error: QtCore.QProcess.ProcessError) -> None:
        if error == QtCore.QProcess.ProcessError.UnknownError:
            return
        self.process_start_timer.stop()
        self._starting_process = False
        self._log(f"[error] process error: {error.name}.")
        self._write_state("process-error")

    def _log(self, message: str) -> None:
        for line in message.splitlines() or [message]:
            entry = LogEntry(level=classify_line(line) or "info", text=line)
            self.all_log_lines.append(entry)
            self.pending_log_entries.append(entry)
        if not self.log_flush_timer.isActive():
            self.log_flush_timer.start(33)
        append_text(DASHBOARD_LOG, message)
        self._write_filtered_logs(message)

    def _refresh_log_view(self) -> None:
        horizontal_scroll = self.output_text.horizontalScrollBar().value()
        html_body = "".join(
            render_log_entry_html(entry)
            for entry in self.all_log_lines
            if self._entry_matches_filter(entry)
        )
        self.output_text.setHtml(
            "<html><body style='background:#000000; margin:6px;'>"
            + html_body
            + "</body></html>"
        )
        cursor = self.output_text.textCursor()
        cursor.movePosition(QtGui.QTextCursor.MoveOperation.End)
        self.output_text.setTextCursor(cursor)
        self.output_text.horizontalScrollBar().setValue(horizontal_scroll)

    def _flush_log_updates(self) -> None:
        if not self.pending_log_entries:
            return
        visible_entries = [
            entry
            for entry in self.pending_log_entries
            if self._entry_matches_filter(entry)
        ]
        self.pending_log_entries.clear()
        if not visible_entries:
            return

        horizontal_scroll = self.output_text.horizontalScrollBar().value()
        cursor = self.output_text.textCursor()
        cursor.movePosition(QtGui.QTextCursor.MoveOperation.End)
        fragment = "".join(render_log_entry_html(entry) for entry in visible_entries)
        cursor.insertHtml(fragment)
        cursor.movePosition(QtGui.QTextCursor.MoveOperation.End)
        self.output_text.setTextCursor(cursor)
        self.output_text.ensureCursorVisible()
        self.output_text.horizontalScrollBar().setValue(horizontal_scroll)

    def _entry_matches_filter(self, entry: LogEntry) -> bool:
        if self.log_filter_all_checkbox.isChecked():
            return True
        active_levels = set()
        if self.log_filter_debug_checkbox.isChecked():
            active_levels.add("debug")
        if self.log_filter_warning_checkbox.isChecked():
            active_levels.add("warning")
        if self.log_filter_error_checkbox.isChecked():
            active_levels.add("error")
        if not active_levels:
            return True
        return entry.level in active_levels

    def _handle_all_filter_toggled(self, checked: bool) -> None:
        if checked:
            for checkbox in (
                self.log_filter_debug_checkbox,
                self.log_filter_warning_checkbox,
                self.log_filter_error_checkbox,
            ):
                previous = checkbox.blockSignals(True)
                checkbox.setChecked(False)
                checkbox.blockSignals(previous)
        self._refresh_log_view()

    def _handle_level_filter_toggled(self, checked: bool) -> None:
        if checked and self.log_filter_all_checkbox.isChecked():
            previous = self.log_filter_all_checkbox.blockSignals(True)
            self.log_filter_all_checkbox.setChecked(False)
            self.log_filter_all_checkbox.blockSignals(previous)

        if (
            not self.log_filter_debug_checkbox.isChecked()
            and not self.log_filter_warning_checkbox.isChecked()
            and not self.log_filter_error_checkbox.isChecked()
        ):
            previous = self.log_filter_all_checkbox.blockSignals(True)
            self.log_filter_all_checkbox.setChecked(True)
            self.log_filter_all_checkbox.blockSignals(previous)

        self._refresh_log_view()

    def _refresh_dashboard(self) -> None:
        preset = self.selected_preset()
        preset_label = self.selected_preset_label()
        current_run = self.current_label or "idle"
        self.status_label.setText(
            f"{ACTIVE_PRODUCT}/{preset_label} • preset {preset}"
            f" • {current_run} • {preset_summary(preset).replace(chr(10), ' • ')}"
            f" • {probe_status_summary(preset)}"
        )
        self.build_button.setText(f"Build {preset_label}")
        self.validate_button.setEnabled(True)
        self.validate_button.setText("Validate")
        self.build_all_button.setText("Build All Presets")
        self.probe_run_button.setText("Build + Start Probe")
        self.doctor_button.setText("List Presets")

    def _continue_probe_run(self, exit_code: int, completed_label: str) -> bool:
        plan = self.pending_probe_run
        if plan is None:
            return False
        if exit_code != 0:
            self._log("[error] probe run aborted because the build step failed.")
            self.pending_probe_run = None
            return True
        if completed_label.startswith("probe run build"):
            run_path = resolve_run_path(plan.preset)
            if run_path is None:
                self._log(
                    f"[error] probe run could not find client launch probe for {ACTIVE_PRODUCT}/{plan.preset}."
                )
                self.pending_probe_run = None
                return True
            self._start_command(
                f"probe run {ACTIVE_PRODUCT}/{plan.preset}", run_path, []
            )
            self.pending_probe_run = None
            return True
        return False

    def _launch_post_command_tools(self, completed_label: str) -> None:
        if not (
            completed_label.startswith("client probe ")
            or completed_label.startswith("probe run ")
        ):
            return
        if self.launch_tracy_checkbox.isChecked():
            self.launch_tracy()
        if self.launch_renderdoc_checkbox.isChecked():
            self.launch_renderdoc()
        if self.capture_tracy_checkbox.isChecked():
            QtCore.QTimer.singleShot(2000, self.capture_tracy)

    def _save_tool_settings(self) -> None:
        self.tool_settings = ToolSettings(
            launch_tracy_with_probe=self.launch_tracy_checkbox.isChecked(),
            launch_renderdoc_with_probe=self.launch_renderdoc_checkbox.isChecked(),
            capture_tracy_with_probe=self.capture_tracy_checkbox.isChecked(),
            tracy_capture_seconds=self.tracy_seconds_spin.value(),
        )
        SETTINGS_PATH.write_text(
            json.dumps(asdict(self.tool_settings), indent=2), encoding="utf-8"
        )

    def _write_filtered_logs(self, message: str) -> None:
        for line in message.splitlines():
            level = classify_line(line)
            if level is None:
                continue
            if level == "warning":
                log_path = WARNINGS_LOG
            elif level == "error":
                log_path = ERRORS_LOG
            else:
                continue
            with log_path.open("a", encoding="utf-8") as handle:
                handle.write(f"[{now_iso()}] {line}\n")

    def _write_state(self, phase: str, exit_code: int | None = None) -> None:
        preset = self.selected_preset()
        state = {
            "updated_at": now_iso(),
            "phase": phase,
            "selected_product": ACTIVE_PRODUCT,
            "selected_preset_label": self.selected_preset_label(),
            "selected_preset": preset,
            "current_label": self.current_label,
            "current_program": self.current_program,
            "current_args": self.current_args,
            "process_id": int(self.process.processId())
            if self.process.processId()
            else None,
            "status_text": self.status_label.text(),
            "warnings_log": str(WARNINGS_LOG),
            "errors_log": str(ERRORS_LOG),
            "dashboard_log": str(DASHBOARD_LOG),
            "configure_script": str(CONFIGURE_SCRIPT),
            "build_script": str(CMAKE_BUILD_SCRIPT),
            "tracy_tool": str(TRACY_TOOL_SCRIPT),
            "renderdoc_tool": str(RENDERDOC_TOOL_SCRIPT),
            "bootstrap_script": str(BOOTSTRAP_SCRIPT),
            "tool_settings": asdict(self.tool_settings),
            "pending_probe_run": asdict(self.pending_probe_run)
            if self.pending_probe_run
            else None,
            "pending_command_after_configure": {
                "label": self.pending_command_after_configure.label,
                "program": str(self.pending_command_after_configure.program),
                "args": self.pending_command_after_configure.args,
            }
            if self.pending_command_after_configure
            else None,
        }
        if exit_code is not None:
            state["exit_code"] = exit_code
        STATE_PATH.write_text(json.dumps(state, indent=2), encoding="utf-8")


def build_application() -> QtWidgets.QApplication:
    app = QtWidgets.QApplication(sys.argv)
    app.setApplicationName("Octaryn Workspace Control")
    return app


def main() -> int:
    if (
        not CMAKE_BUILD_SCRIPT.exists()
        or not CONFIGURE_SCRIPT.exists()
        or not TRACY_TOOL_SCRIPT.exists()
        or not RENDERDOC_TOOL_SCRIPT.exists()
        or not BOOTSTRAP_SCRIPT.exists()
    ):
        print(
            "Expected Octaryn root tools were not found under tools/.",
            file=sys.stderr,
        )
        return 1

    app = build_application()
    window = EngineControlWindow()
    window.show()
    return app.exec()


if __name__ == "__main__":
    raise SystemExit(main())
