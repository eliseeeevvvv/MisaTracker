
import { useCallback, useEffect, useState } from "react";
import "./TaskManager.css";

const API_BASE = "https://misatracker-api.onrender.com";

const API = {
    tasks: `${API_BASE}/api/tasks`,
    subjects: `${API_BASE}/api/subjects`,
    folders: `${API_BASE}/api/folders`
};

function authHeaders() {
    return {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("misaToken") || ""}`
    };
}

async function apiRequest(url, options = {}) {
    console.log("API REQUEST:", url);
    const response = await fetch(url, {
        ...options,
        headers: {
            ...authHeaders(),
            ...options.headers
        }
    });

    const data = await response.json().catch(() => null);

    if (!response.ok) {
        throw new Error(
            data?.message || `Ошибка запроса: ${response.status}`
        );
    }

    return data;
}

// Преобразует дату API в локальный формат для отображения.
// Если backend вернул дату без указания часового пояса,
// она рассматривается как локальная дата и время.
function formatDate(value) {
    if (!value) return "Срок не указан";

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
        return "Некорректная дата";
    }

    return date.toLocaleString("ru-RU", {
        dateStyle: "medium",
        timeStyle: "short"
    });
}

export default function TaskManager() {
    const [tasks, setTasks] = useState([]);
    const [subjects, setSubjects] = useState([]);
    const [folders, setFolders] = useState([]);

    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [dueDate, setDueDate] = useState("");
    const [subjectId, setSubjectId] = useState("");
    const [folderId, setFolderId] = useState("");
    const [folderName, setFolderName] = useState("");

    const [loading, setLoading] = useState(false);
    const [folderLoading, setFolderLoading] = useState(false);
    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");

    const loadTasks = useCallback(async () => {
        const data = await apiRequest(API.tasks);
        setTasks(Array.isArray(data) ? data : []);
    }, []);

    const loadSubjects = useCallback(async () => {
        const data = await apiRequest(API.subjects);
        const list = Array.isArray(data) ? data : [];

        setSubjects(list);

        setSubjectId(current => {
            if (list.some(s => String(s.id) === current)) {
                return current;
            }

            return list.length ? String(list[0].id) : "";
        });

        return list;
    }, []);

    const loadFolders = useCallback(async (id) => {
        if (!id) {
            setFolders([]);
            setFolderId("");
            return [];
        }

        const data = await apiRequest(
            `${API.folders}?subjectId=${encodeURIComponent(id)}`
        );

        const list = Array.isArray(data) ? data : [];
        setFolders(list);

        setFolderId(current => {
            if (list.some(f => String(f.id) === current)) {
                return current;
            }

            return list.length ? String(list[0].id) : "";
        });

        return list;
    }, []);

    useEffect(() => {
        let cancelled = false;

        async function initialize() {
            const results = await Promise.allSettled([
                apiRequest(API.subjects),
                apiRequest(API.tasks)
            ]);

            if (cancelled) return;

            const [subjectsResult, tasksResult] = results;

            if (subjectsResult.status === "fulfilled") {
                const subjectList = Array.isArray(subjectsResult.value)
                    ? subjectsResult.value
                    : [];

                setSubjects(subjectList);

                const firstId = subjectList.length
                    ? String(subjectList[0].id)
                    : "";

                setSubjectId(firstId);

                if (firstId) {
                    try {
                        const data = await apiRequest(
                            `${API.folders}?subjectId=${encodeURIComponent(firstId)}`
                        );

                        if (!cancelled) {
                            const list = Array.isArray(data) ? data : [];
                            setFolders(list);
                            setFolderId(
                                list.length ? String(list[0].id) : ""
                            );
                        }
                    } catch (err) {
                        if (!cancelled) {
                            console.error("Ошибка загрузки папок:", err);
                            setError(err.message);
                        }
                    }
                }
            } else {
                console.error(
                    "Ошибка загрузки предметов:",
                    subjectsResult.reason
                );

                setError(
                    subjectsResult.reason?.message ||
                    "Не удалось загрузить предметы."
                );
            }

            if (tasksResult.status === "fulfilled") {
                if (!cancelled) {
                    setTasks(
                        Array.isArray(tasksResult.value)
                            ? tasksResult.value
                            : []
                    );
                }
            } else {
                console.error("Ошибка загрузки заданий:", tasksResult.reason);

                if (!cancelled) {
                    setTasks([]);
                    setError(
                        "Предметы загружены, но задания недоступны. Проверь API /api/tasks."
                    );
                }
            }
        }

        initialize();

        return () => {
            cancelled = true;
        };
    }, []);

    async function handleSubjectChange(value) {
        setSubjectId(value);
        setFolderId("");
        setFolders([]);
        setError("");
        setSuccess("");

        try {
            await loadFolders(value);
        } catch (err) {
            setError(err.message || "Не удалось загрузить папки.");
        }
    }

    async function addFolder(event) {
        event.preventDefault();
        setError("");
        setSuccess("");

        if (!subjectId) {
            setError("Сначала выбери предмет.");
            return;
        }

        if (!folderName.trim()) {
            setError("Введи название папки.");
            return;
        }

        try {
            setFolderLoading(true);

            await apiRequest(API.folders, {
                method: "POST",
                body: JSON.stringify({
                    name: folderName.trim(),
                    subjectId: Number(subjectId)
                })
            });

            setFolderName("");
            await loadFolders(subjectId);
            setSuccess("Папка создана! 📁");
        } catch (err) {
            setError(err.message || "Не удалось создать папку.");
        } finally {
            setFolderLoading(false);
        }
    }

    async function addTask(event) {
        event.preventDefault();
        setError("");
        setSuccess("");

        if (!name.trim()) {
            setError("Введи название задачи.");
            return;
        }

        if (!dueDate) {
            setError("Укажи срок выполнения.");
            return;
        }

        if (!subjectId) {
            setError("Сначала создай и выбери предмет.");
            return;
        }

        if (!folderId) {
            setError("Сначала создай и выбери папку.");
            return;
        }

        try {
            setLoading(true);

            // datetime-local уже содержит локальные дату и время.
            // Не преобразуем значение через toISOString(), чтобы
            // не сдвигать время на часовой пояс UTC.
            const localDueDate = `${dueDate}:00`;

            await apiRequest(API.tasks, {
                method: "POST",
                body: JSON.stringify({
                    name: name.trim(),
                    description: description.trim(),
                    dueDate: localDueDate,
                    subjectId: Number(subjectId),
                    folderId: Number(folderId)
                })
            });

            setName("");
            setDescription("");
            setDueDate("");

            try {
                await loadTasks();
            } catch (err) {
                console.error(
                    "Задача создана, но список не обновился:",
                    err
                );
            }

            setSuccess("Задача добавлена!");
        } catch (err) {
            setError(err.message || "Не удалось создать задачу.");
        } finally {
            setLoading(false);
        }
    }

    async function toggleTask(task) {
        setError("");
        setSuccess("");

        try {
            await apiRequest(`${API.tasks}/${task.id}/completion`, {
                method: "PATCH",
                body: JSON.stringify({
                    isCompleted: !task.isCompleted
                })
            });

            await loadTasks();
        } catch (err) {
            setError(err.message || "Не удалось изменить задачу.");
        }
    }

    async function deleteTask(id) {
        if (!window.confirm("Удалить эту задачу?")) return;

        setError("");
        setSuccess("");

        try {
            await apiRequest(`${API.tasks}/${id}`, {
                method: "DELETE"
            });

            setTasks(current =>
                current.filter(task => task.id !== id)
            );

            setSuccess("Задача удалена.");
        } catch (err) {
            setError(err.message || "Не удалось удалить задачу.");
        }
    }

    async function refreshAll() {
        setError("");
        setSuccess("");

        const results = await Promise.allSettled([
            loadSubjects(),
            loadTasks()
        ]);

        if (results[0].status === "rejected") {
            setError(
                results[0].reason?.message ||
                "Не удалось загрузить предметы."
            );
        }

        if (results[1].status === "rejected") {
            setTasks([]);
            setError("Не удалось загрузить задания.");
        }

        try {
            if (subjectId) await loadFolders(subjectId);
        } catch (err) {
            setError(err.message || "Не удалось обновить папки.");
        }
    }

    const unfinishedCount = tasks.filter(
        task => !task.isCompleted
    ).length;

    return (
        <section className="task-manager">
            <header className="task-header">
                <div>
                    <p className="task-eyebrow">ТВОЙ ПЛАН НА ДЕНЬ</p>
                    <h2>Мои задачи</h2>
                    <p className="task-subtitle">
                        Маленькими шагами к большим целям
                    </p>
                </div>

                <div className="task-counter">
                    <strong>{unfinishedCount}</strong>
                    <span>осталось</span>
                </div>
            </header>

            {error && (
                <div className="task-error" role="alert">{error}</div>
            )}

            {success && (
                <div className="task-success" role="status">{success}</div>
            )}

            <form className="task-form" onSubmit={addFolder}>
                <h3>📁 Новая папка</h3>

                <label>
                    Предмет
                    <select
                        value={subjectId}
                        onChange={event =>
                            handleSubjectChange(event.target.value)
                        }
                        required
                    >
                        <option value="">Выбери предмет</option>
                        {subjects.map(subject => (
                            <option
                                key={subject.id}
                                value={String(subject.id)}
                            >
                                {subject.name}
                            </option>
                        ))}
                    </select>
                </label>

                {subjects.length === 0 && (
                    <p className="task-hint">
                        Сначала создай предмет в разделе «Предметы».
                    </p>
                )}

                <label>
                    Название папки
                    <input
                        value={folderName}
                        onChange={event =>
                            setFolderName(event.target.value)
                        }
                        placeholder="Например, Домашние задания"
                        maxLength={100}
                        required
                    />
                </label>

                <button
                    className="task-add-button"
                    type="submit"
                    disabled={folderLoading || !subjectId}
                >
                    {folderLoading ? "Создание..." : "+ Создать папку"}
                </button>
            </form>

            <form className="task-form" onSubmit={addTask}>
                <h3>🍓 Новая задача</h3>

                <label>
                    Название
                    <input
                        value={name}
                        onChange={event => setName(event.target.value)}
                        placeholder="Например, сделать домашнее задание"
                        maxLength={100}
                        required
                    />
                </label>

                <label>
                    Описание
                    <textarea
                        value={description}
                        onChange={event =>
                            setDescription(event.target.value)
                        }
                        placeholder="Что нужно сделать?"
                        maxLength={2000}
                    />
                </label>

                <label>
                    Предмет
                    <select
                        value={subjectId}
                        onChange={event =>
                            handleSubjectChange(event.target.value)
                        }
                        required
                    >
                        <option value="">Выбери предмет</option>
                        {subjects.map(subject => (
                            <option
                                key={subject.id}
                                value={String(subject.id)}
                            >
                                {subject.name}
                            </option>
                        ))}
                    </select>
                </label>

                <label>
                    Папка
                    <select
                        value={folderId}
                        onChange={event => setFolderId(event.target.value)}
                        disabled={!subjectId || folders.length === 0}
                        required
                    >
                        <option value="">Выбери папку</option>
                        {folders.map(folder => (
                            <option
                                key={folder.id}
                                value={String(folder.id)}
                            >
                                {folder.name}
                            </option>
                        ))}
                    </select>
                </label>

                {subjectId && folders.length === 0 && (
                    <p className="task-hint">
                        Создай папку выше для выбранного предмета.
                    </p>
                )}

                <label>
                    Срок выполнения
                    <input
                        type="datetime-local"
                        value={dueDate}
                        onChange={event => setDueDate(event.target.value)}
                        required
                    />
                </label>

                <button
                    className="task-add-button"
                    type="submit"
                    disabled={loading || !subjects.length || !folders.length}
                >
                    {loading ? "Добавление..." : "+ Добавить задачу"}
                </button>
            </form>

            <div className="task-list-header">
                <h3>Список задач</h3>
                <button type="button" onClick={refreshAll}>
                    Обновить
                </button>
            </div>

            <div className="task-list">
                {tasks.length === 0 ? (
                    <div className="task-empty">
                        <span>🌼</span>
                        <p>
                            Пока нет задач. Самое время добавить первую!
                        </p>
                    </div>
                ) : (
                    tasks.map(task => {
                        const subject = subjects.find(
                            item =>
                                Number(item.id) === Number(task.subjectId)
                        );

                        const folder = folders.find(
                            item =>
                                Number(item.id) === Number(task.folderId)
                        );

                        return (
                            <article
                                className={`task-card ${task.isCompleted ? "completed" : ""
                                    }`}
                                key={task.id}
                            >
                                <button
                                    type="button"
                                    className={`task-check ${task.isCompleted ? "checked" : ""
                                        }`}
                                    onClick={() => toggleTask(task)}
                                    aria-label={
                                        task.isCompleted
                                            ? "Отметить невыполненной"
                                            : "Отметить выполненной"
                                    }
                                >
                                    {task.isCompleted ? "✓" : ""}
                                </button>

                                <div className="task-content">
                                    <h4>{task.name}</h4>

                                    {task.description && (
                                        <p>{task.description}</p>
                                    )}

                                    <span className="task-due">
                                        {formatDate(task.dueDate)}
                                    </span>

                                    <span className="task-meta">
                                        {subject?.name ||
                                            `Предмет #${task.subjectId}`}
                                        {" · "}
                                        {folder?.name ||
                                            `Папка #${task.folderId}`}
                                    </span>
                                </div>

                                <button
                                    type="button"
                                    className="task-delete"
                                    onClick={() => deleteTask(task.id)}
                                    aria-label="Удалить задачу"
                                    title="Удалить"
                                >
                                    ×
                                </button>
                            </article>
                        );
                    })
                )}
            </div>
        </section>
    );
}
