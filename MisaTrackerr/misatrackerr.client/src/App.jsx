
import { useEffect, useState } from "react";

import TaskManager from "./components/TaskManager";
import { enablePushNotifications } from "./pushNotifications";
import "./App.css";

const API_BASE = "https://misatracker-api.onrender.com";

const API = {
    subjects: `${API_BASE}/api/subjects`,
    tasks: `${API_BASE}/api/tasks`,
    lessons: `${API_BASE}/api/lessons`,
    auth: `${API_BASE}/api/auth`
};

export default function App() {
    const [token, setToken] = useState(
        () => localStorage.getItem("misaToken") || ""
    );

    const [user, setUser] = useState(() => {
        try {
            return JSON.parse(localStorage.getItem("misaUser") || "null");
        } catch {
            return null;
        }
    });

    const [page, setPage] = useState(() =>
        localStorage.getItem("misaToken") ? "home" : "login"
    );

    const [tab, setTab] = useState("subjects");
    const [subjects, setSubjects] = useState([]);
    const [tasks, setTasks] = useState([]);
    const [lessons, setLessons] = useState([]);
    const [selectedSubject, setSelectedSubject] = useState(null);

    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    const [subjectName, setSubjectName] = useState("");
    const [lessonName, setLessonName] = useState("");
    const [lessonDescription, setLessonDescription] = useState("");
    const [lessonStart, setLessonStart] = useState("");
    const [lessonEnd, setLessonEnd] = useState("");

    const [error, setError] = useState("");
    const [message, setMessage] = useState("");
    const [loading, setLoading] = useState(false);
    const [pushLoading, setPushLoading] = useState(false);

    const [currentTime, setCurrentTime] = useState(0);

    useEffect(() => {
        function updateTime() {
            setCurrentTime(Date.now());
        }

        updateTime();

        const timer = setInterval(updateTime, 60 * 1000);

        return () => clearInterval(timer);
    }, []);

    async function apiRequest(url, options = {}) {
        const response = await fetch(url, {
            ...options,
            headers: {
                ...(options.body
                    ? { "Content-Type": "application/json" }
                    : {}),
                ...(token
                    ? { Authorization: `Bearer ${token}` }
                    : {}),
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

    async function loadData() {
        if (!token) return;

        const results = await Promise.allSettled([
            apiRequest(API.subjects),
            apiRequest(API.tasks),
            apiRequest(API.lessons)
        ]);

        const [subjectResult, taskResult, lessonResult] = results;

        if (subjectResult.status === "fulfilled") {
            setSubjects(
                Array.isArray(subjectResult.value)
                    ? subjectResult.value
                    : []
            );
        } else {
            console.error("Ошибка предметов:", subjectResult.reason);
            setError("Не удалось загрузить предметы.");
        }

        if (taskResult.status === "fulfilled") {
            setTasks(
                Array.isArray(taskResult.value)
                    ? taskResult.value
                    : []
            );
        } else {
            console.error("Ошибка заданий:", taskResult.reason);
            setTasks([]);
        }

        if (lessonResult.status === "fulfilled") {
            setLessons(
                Array.isArray(lessonResult.value)
                    ? lessonResult.value
                    : []
            );
        } else {
            console.error("Ошибка расписания:", lessonResult.reason);
            setLessons([]);
        }
    }

    useEffect(() => {
        if (!token || !user || page !== "home") return;

        let cancelled = false;

        async function fetchData() {
            const results = await Promise.allSettled([
                apiRequest(API.subjects),
                apiRequest(API.tasks),
                apiRequest(API.lessons)
            ]);

            if (cancelled) return;

            const [subjectResult, taskResult, lessonResult] = results;

            if (subjectResult.status === "fulfilled") {
                setSubjects(
                    Array.isArray(subjectResult.value)
                        ? subjectResult.value
                        : []
                );
            } else {
                console.error("Ошибка предметов:", subjectResult.reason);
            }

            if (taskResult.status === "fulfilled") {
                setTasks(
                    Array.isArray(taskResult.value)
                        ? taskResult.value
                        : []
                );
            } else {
                console.error("Ошибка заданий:", taskResult.reason);
                setTasks([]);
            }

            if (lessonResult.status === "fulfilled") {
                setLessons(
                    Array.isArray(lessonResult.value)
                        ? lessonResult.value
                        : []
                );
            } else {
                console.error("Ошибка расписания:", lessonResult.reason);
                setLessons([]);
            }
        }

        fetchData();

        return () => {
            cancelled = true;
        };
    }, [token, user, page]);

    async function handleAuth(event) {
        event.preventDefault();
        setError("");
        setMessage("");
        setLoading(true);

        try {
            const isRegister = page === "register";

            const response = await fetch(
                `${API.auth}/${isRegister ? "register" : "login"}`,
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify(
                        isRegister
                            ? { name, email, password }
                            : { email, password }
                    )
                }
            );

            const data = await response.json().catch(() => ({}));

            if (!response.ok) {
                throw new Error(
                    data.message || "Не удалось выполнить запрос."
                );
            }

            if (isRegister) {
                setMessage(
                    "Регистрация успешна! Теперь войди в аккаунт."
                );
                setPage("login");
                setName("");
                setPassword("");
                return;
            }

            if (!data.token || !data.user) {
                throw new Error(
                    "Сервер не вернул токен авторизации."
                );
            }

            localStorage.setItem("misaToken", data.token);
            localStorage.setItem(
                "misaUser",
                JSON.stringify(data.user)
            );

            setToken(data.token);
            setUser(data.user);
            setPassword("");
            setPage("home");
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }

    function logout() {
        localStorage.removeItem("misaToken");
        localStorage.removeItem("misaUser");
        localStorage.removeItem("misaLoggedIn");
        localStorage.removeItem("misaEmail");

        setToken("");
        setUser(null);
        setEmail("");
        setPassword("");
        setError("");
        setMessage("");
        setPage("login");
        setTab("subjects");
        setSelectedSubject(null);
        setSubjects([]);
        setTasks([]);
        setLessons([]);
    }

    async function handleEnablePush() {
        setError("");
        setMessage("");
        setPushLoading(true);

        try {
            const result = await enablePushNotifications();
            setMessage(result);
        } catch (err) {
            setError(
                err.message || "Не удалось включить уведомления."
            );
        } finally {
            setPushLoading(false);
        }
    }

    async function addSubject(event) {
        event.preventDefault();
        if (!subjectName.trim()) return;

        setError("");
        setMessage("");

        try {
            await apiRequest(API.subjects, {
                method: "POST",
                body: JSON.stringify({
                    name: subjectName.trim()
                })
            });

            setSubjectName("");
            setMessage("Предмет добавлен! 🌷");
            await loadData();
        } catch (err) {
            setError(err.message);
        }
    }

    async function deleteSubject(subject) {
        if (!window.confirm(`Удалить предмет «${subject.name}»?`)) {
            return;
        }

        setError("");
        setMessage("");

        try {
            await apiRequest(`${API.subjects}/${subject.id}`, {
                method: "DELETE"
            });

            if (selectedSubject?.id === subject.id) {
                setSelectedSubject(null);
            }

            setMessage("Предмет удалён.");
            await loadData();
        } catch (err) {
            setError(err.message);
        }
    }

    async function addLesson(event) {
        event.preventDefault();
        setError("");
        setMessage("");

        if (!lessonName.trim()) {
            setError("Укажи название урока.");
            return;
        }

        if (!lessonStart || !lessonEnd) {
            setError("Укажи дату и время урока.");
            return;
        }

        if (new Date(lessonEnd) <= new Date(lessonStart)) {
            setError("Окончание должно быть позже начала.");
            return;
        }

        try {
            await apiRequest(API.lessons, {
                method: "POST",
                body: JSON.stringify({
                    name: lessonName.trim(),
                    description: lessonDescription.trim(),
                    startDate: new Date(lessonStart).toISOString(),
                    endDate: new Date(lessonEnd).toISOString()
                })
            });

            setLessonName("");
            setLessonDescription("");
            setLessonStart("");
            setLessonEnd("");
            setMessage("Урок добавлен в расписание! 📅");

            await loadData();
        } catch (err) {
            setError(err.message);
        }
    }

    async function deleteLesson(id) {
        if (!window.confirm("Удалить этот урок?")) return;

        try {
            await apiRequest(`${API.lessons}/${id}`, {
                method: "DELETE"
            });

            setLessons(current =>
                current.filter(lesson => lesson.id !== id)
            );
            setMessage("Урок удалён.");
        } catch (err) {
            setError(err.message);
        }
    }

    function formatDate(value) {
        if (!value) return "Дата не указана";

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) {
            return "Некорректная дата";
        }

        return date.toLocaleString("ru-RU", {
            day: "2-digit",
            month: "long",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit"
        });
    }

    const oneDay = 24 * 60 * 60 * 1000;

    const deadlineTasks = tasks
        .filter(task => !task.isCompleted)
        .map(task => ({
            ...task,
            deadlineTime: new Date(task.dueDate).getTime()
        }))
        .filter(task =>
            currentTime > 0 &&
            Number.isFinite(task.deadlineTime) &&
            task.deadlineTime <= currentTime + oneDay
        )
        .sort((a, b) => a.deadlineTime - b.deadlineTime);

    function getDeadlineMessage(task) {
        const remaining = task.deadlineTime - currentTime;

        if (remaining < 0) {
            return "⚠️ Срок уже истёк!";
        }

        if (remaining <= 60 * 60 * 1000) {
            return "🔴 Дедлайн менее чем через час!";
        }

        return "🍓 Дедлайн в ближайшие 24 часа!";
    }

    if (!token || !user || page !== "home") {
        return (
            <main className="auth-page">
                <section className="auth-card">
                    <div className="auth-logo">🍓</div>
                    <h1>MisaTracker</h1>
                    <p className="auth-subtitle">
                        Маленькими кроками до великих цілей ♡
                    </p>

                    <div className="auth-tabs">
                        <button
                            type="button"
                            className={page === "login" ? "active" : ""}
                            onClick={() => {
                                setPage("login");
                                setError("");
                                setMessage("");
                            }}
                        >
                            Sign in
                        </button>

                        <button
                            type="button"
                            className={page === "register" ? "active" : ""}
                            onClick={() => {
                                setPage("register");
                                setError("");
                                setMessage("");
                            }}
                        >
                            Sign up
                        </button>
                    </div>

                    <form onSubmit={handleAuth} className="auth-form">
                        {page === "register" && (
                            <label>
                                Имя
                                <input
                                    value={name}
                                    onChange={e => setName(e.target.value)}
                                    minLength={2}
                                    maxLength={50}
                                    autoComplete="name"
                                    required
                                />
                            </label>
                        )}

                        <label>
                            Email
                            <input
                                type="email"
                                value={email}
                                onChange={e => setEmail(e.target.value)}
                                autoComplete="email"
                                required
                            />
                        </label>

                        <label>
                            Пароль
                            <input
                                type="password"
                                value={password}
                                onChange={e => setPassword(e.target.value)}
                                minLength={8}
                                autoComplete={
                                    page === "register"
                                        ? "new-password"
                                        : "current-password"
                                }
                                required
                            />
                        </label>

                        {error && (
                            <p className="auth-error">{error}</p>
                        )}

                        {message && (
                            <p className="auth-message">{message}</p>
                        )}

                        <button
                            className="auth-submit"
                            type="submit"
                            disabled={loading}
                        >
                            {loading
                                ? "Подожди..."
                                : page === "register"
                                    ? "Create account"
                                    : "Sign in"}
                        </button>
                    </form>

                    <p className="auth-footer">
                        Учись в своём темпе 🌷
                    </p>
                </section>
            </main>
        );
    }

    const subjectTasks = selectedSubject
        ? tasks.filter(
            task => Number(task.subjectId) === Number(selectedSubject.id)
        )
        : [];

    return (
        <main className="app">
            <header className="app-header">
                <div>
                    <h1>MisaTracker ♡</h1>
                    <p>Привет, {user.name || user.email}! 🐸</p>
                </div>

                <button className="logout-button" onClick={logout}>
                    Выйти
                </button>
            </header>

            <nav className="app-navigation">
                <button
                    className={tab === "subjects" ? "active" : ""}
                    onClick={() => {
                        setTab("subjects");
                        setSelectedSubject(null);
                    }}
                >
                    📚 Предметы
                </button>

                <button
                    className={tab === "tasks" ? "active" : ""}
                    onClick={() => {
                        setTab("tasks");
                        setSelectedSubject(null);
                    }}
                >
                    🍓 Задания
                </button>

                <button
                    className={tab === "schedule" ? "active" : ""}
                    onClick={() => {
                        setTab("schedule");
                        setSelectedSubject(null);
                    }}
                >
                    📅 Расписание
                </button>
            </nav>

            {error && <p className="dashboard-error">{error}</p>}

            {deadlineTasks.length > 0 && (
                <section className="deadline-notifications">
                    <h2>⏰ Скоро дедлайны!</h2>

                    {deadlineTasks.map(task => (
                        <article
                            className="deadline-notice"
                            key={task.id}
                        >
                            <strong>🍓 {task.name}</strong>
                            <p>{getDeadlineMessage(task)}</p>
                            <small>
                                Срок: {formatDate(task.dueDate)}
                            </small>
                        </article>
                    ))}
                </section>
            )}

            {message && <p className="dashboard-message">{message}</p>}

            <div className="dashboard-actions">
                <button onClick={loadData}>
                    ↻ Обновить данные
                </button>

                <button
                    onClick={handleEnablePush}
                    disabled={pushLoading}
                >
                    {pushLoading
                        ? "Подключаем..."
                        : "🔔 Включить уведомления"}
                </button>
            </div>

            {tab === "subjects" && (
                <section className="dashboard-section">
                    {selectedSubject ? (
                        <>
                            <button
                                className="back-button"
                                onClick={() => setSelectedSubject(null)}
                            >
                                ← Все предметы
                            </button>

                            <h2>🌷 {selectedSubject.name}</h2>
                            <p>Задания по этому предмету</p>

                            {subjectTasks.length === 0 ? (
                                <p className="empty-state">
                                    Пока нет заданий по этому предмету 🌱
                                </p>
                            ) : (
                                <div className="dashboard-list">
                                    {subjectTasks.map(task => (
                                        <article
                                            className="dashboard-card"
                                            key={task.id}
                                        >
                                            <h3>
                                                {task.isCompleted ? "✅" : "🍓"}{" "}
                                                {task.name}
                                            </h3>

                                            {task.description && (
                                                <p>{task.description}</p>
                                            )}

                                            <small>
                                                Срок: {formatDate(task.dueDate)}
                                            </small>
                                        </article>
                                    ))}
                                </div>
                            )}
                        </>
                    ) : (
                        <>
                            <h2>Мои предметы 📚</h2>
                            <p>
                                Выбирай предмет, чтобы увидеть его задания.
                            </p>

                            <form
                                className="dashboard-form"
                                onSubmit={addSubject}
                            >
                                <input
                                    value={subjectName}
                                    onChange={e =>
                                        setSubjectName(e.target.value)
                                    }
                                    placeholder="Название предмета"
                                    maxLength={100}
                                    required
                                />

                                <button type="submit">
                                    + Добавить предмет
                                </button>
                            </form>

                            {subjects.length === 0 ? (
                                <p className="empty-state">
                                    Здесь пока пусто. Добавь первый предмет 🌱
                                </p>
                            ) : (
                                <div className="dashboard-list">
                                    {subjects.map(subject => (
                                        <article
                                            className="dashboard-card subject-card"
                                            key={subject.id}
                                        >
                                            <button
                                                type="button"
                                                className="subject-open"
                                                onClick={() =>
                                                    setSelectedSubject(subject)
                                                }
                                            >
                                                <span className="subject-emoji">
                                                    🌷
                                                </span>

                                                <span>
                                                    <strong>
                                                        {subject.name}
                                                    </strong>

                                                    <small>
                                                        Заданий:{" "}
                                                        {subject.taskCount ?? 0}
                                                        {" · "}
                                                        Папок:{" "}
                                                        {subject.folderCount ?? 0}
                                                    </small>
                                                </span>

                                                <span>→</span>
                                            </button>

                                            <button
                                                type="button"
                                                className="delete-button"
                                                onClick={() =>
                                                    deleteSubject(subject)
                                                }
                                            >
                                                Удалить
                                            </button>
                                        </article>
                                    ))}
                                </div>
                            )}
                        </>
                    )}
                </section>
            )}

            {tab === "tasks" && (
                <section className="dashboard-section">
                    <h2>Мои задания 🍓</h2>
                    <p>Создавай задания и отмечай выполненные.</p>
                    <TaskManager />
                </section>
            )}

            {tab === "schedule" && (
                <section className="dashboard-section">
                    <h2>Расписание уроков 📅</h2>
                    <p>Добавляй занятия с датой и временем.</p>

                    <form
                        className="dashboard-form lesson-form"
                        onSubmit={addLesson}
                    >
                        <input
                            value={lessonName}
                            onChange={e => setLessonName(e.target.value)}
                            placeholder="Название урока"
                            maxLength={100}
                            required
                        />

                        <textarea
                            value={lessonDescription}
                            onChange={e =>
                                setLessonDescription(e.target.value)
                            }
                            placeholder="Описание (необязательно)"
                            maxLength={2000}
                        />

                        <label>
                            Начало
                            <input
                                type="datetime-local"
                                value={lessonStart}
                                onChange={e => setLessonStart(e.target.value)}
                                required
                            />
                        </label>

                        <label>
                            Окончание
                            <input
                                type="datetime-local"
                                value={lessonEnd}
                                onChange={e => setLessonEnd(e.target.value)}
                                required
                            />
                        </label>

                        <button type="submit">
                            + Добавить урок
                        </button>
                    </form>

                    {lessons.length === 0 ? (
                        <p className="empty-state">
                            Расписание пока пустое. Добавь первый урок 🌼
                        </p>
                    ) : (
                        <div className="dashboard-list">
                            {lessons.map(lesson => (
                                <article
                                    className="dashboard-card lesson-card"
                                    key={lesson.id}
                                >
                                    <h3>📖 {lesson.name}</h3>

                                    {lesson.description && (
                                        <p>{lesson.description}</p>
                                    )}

                                    <p>
                                        <strong>Начало:</strong>{" "}
                                        {formatDate(lesson.startDate)}
                                    </p>

                                    <p>
                                        <strong>Окончание:</strong>{" "}
                                        {formatDate(lesson.endDate)}
                                    </p>

                                    <button
                                        className="delete-button"
                                        onClick={() => deleteLesson(lesson.id)}
                                    >
                                        Удалить урок
                                    </button>
                                </article>
                            ))}
                        </div>
                    )}
                </section>
            )}
        </main>
    );
}
