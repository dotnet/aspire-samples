import { useState, useEffect, useCallback } from 'react';
import aspireLogo from '/Aspire.png';
import './App.css';

interface Todo {
  id: string;
  description: string;
  userId: string;
  isComplete: boolean;
}

const USER_ID = 'sampleuser';

function App() {
  const [todos, setTodos] = useState<Todo[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [newTodoDescription, setNewTodoDescription] = useState('');

  const fetchTodos = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/todos');

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data: Todo[] = await response.json();
      setTodos(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch TODO items');
      console.error('Error fetching todos:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchTodos();
  }, [fetchTodos]);

  const createTodo = async () => {
    if (!newTodoDescription.trim()) return;

    try {
      const response = await fetch('/api/todos', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          description: newTodoDescription.trim(),
          userId: USER_ID,
        }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      setNewTodoDescription('');
      await fetchTodos();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create TODO');
    }
  };

  const toggleTodo = async (todo: Todo) => {
    try {
      const response = await fetch(`/api/todos/${todo.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          ...todo,
          isComplete: !todo.isComplete,
        }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      await fetchTodos();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update TODO');
    }
  };

  const deleteTodo = async (todo: Todo) => {
    try {
      const response = await fetch(`/api/todos/${todo.userId}/${todo.id}`, {
        method: 'DELETE',
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      await fetchTodos();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete TODO');
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      createTodo();
    }
  };

  return (
    <div className="app-container">
      <header className="app-header">
        <a
          href="https://aspire.dev"
          target="_blank"
          rel="noopener noreferrer"
          aria-label="Visit Aspire website (opens in new tab)"
          className="logo-link"
        >
          <img src={aspireLogo} className="logo" alt="Aspire logo" />
        </a>
        <h1 className="app-title">Aspire TODO App</h1>
        <p className="app-subtitle">Cosmos DB + Python + React</p>
      </header>

      <main className="main-content">
        <section className="todo-section" aria-labelledby="todo-heading">
          <div className="card">
            <div className="section-header">
              <h2 id="todo-heading" className="section-title">TODO Items</h2>
              <div className="header-actions">
                <button
                  className="refresh-button"
                  onClick={fetchTodos}
                  disabled={loading}
                  aria-label={loading ? 'Loading TODO items' : 'Refresh TODO items'}
                  type="button"
                >
                  <svg
                    className={`refresh-icon ${loading ? 'spinning' : ''}`}
                    width="20"
                    height="20"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    aria-hidden="true"
                    focusable="false"
                  >
                    <path d="M21.5 2v6h-6M2.5 22v-6h6M2 11.5a10 10 0 0 1 18.8-4.3M22 12.5a10 10 0 0 1-18.8 4.2"/>
                  </svg>
                  <span>{loading ? 'Loading...' : 'Refresh'}</span>
                </button>
              </div>
            </div>

            {error && (
              <div className="error-message" role="alert" aria-live="polite">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                  <circle cx="12" cy="12" r="10"/>
                  <line x1="12" y1="8" x2="12" y2="12"/>
                  <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span>{error}</span>
              </div>
            )}

            <div className="todo-input-group">
              <input
                type="text"
                className="todo-input"
                placeholder="What needs to be done?"
                value={newTodoDescription}
                onChange={(e) => setNewTodoDescription(e.target.value)}
                onKeyDown={handleKeyDown}
                aria-label="New TODO description"
              />
              <button
                className="create-button"
                onClick={createTodo}
                disabled={!newTodoDescription.trim()}
                type="button"
              >
                Create
              </button>
            </div>

            {loading && todos.length === 0 && (
              <div className="loading-skeleton" role="status" aria-live="polite" aria-label="Loading TODO items">
                {[...Array(3)].map((_, i) => (
                  <div key={i} className="skeleton-row" aria-hidden="true" />
                ))}
                <span className="visually-hidden">Loading TODO items...</span>
              </div>
            )}

            {!loading && todos.length === 0 && (
              <p className="empty-message">No TODO items yet. Create one above!</p>
            )}

            {todos.length > 0 && (
              <div className="todo-list">
                {todos.map((todo) => (
                  <article
                    key={todo.id}
                    className={`todo-card ${todo.isComplete ? 'completed' : ''}`}
                    aria-label={`TODO: ${todo.description}`}
                  >
                    <div className="todo-content">
                      <span className={`todo-description ${todo.isComplete ? 'done' : ''}`}>
                        {todo.description}
                      </span>
                    </div>
                    <div className="todo-actions">
                      <button
                        className={`toggle-button ${todo.isComplete ? 'undo' : 'complete'}`}
                        onClick={() => toggleTodo(todo)}
                        type="button"
                        aria-label={todo.isComplete ? `Mark "${todo.description}" as unfinished` : `Mark "${todo.description}" as finished`}
                      >
                        {todo.isComplete ? 'Unfinished' : 'Finished'}
                      </button>
                      {todo.isComplete && (
                        <button
                          className="delete-button"
                          onClick={() => deleteTodo(todo)}
                          type="button"
                          aria-label={`Delete "${todo.description}"`}
                        >
                          Delete
                        </button>
                      )}
                    </div>
                  </article>
                ))}
              </div>
            )}
          </div>
        </section>
      </main>

      <footer className="app-footer">
        <nav aria-label="Footer navigation">
          <a href="https://aspire.dev" target="_blank" rel="noopener noreferrer">
            Learn more about Aspire<span className="visually-hidden"> (opens in new tab)</span>
          </a>
          <a
            href="https://github.com/dotnet/aspire"
            target="_blank"
            rel="noopener noreferrer"
            className="github-link"
            aria-label="View Aspire on GitHub (opens in new tab)"
          >
            <img src="/github.svg" alt="" width="24" height="24" aria-hidden="true" />
            <span className="visually-hidden">GitHub</span>
          </a>
        </nav>
      </footer>
    </div>
  );
}

export default App;
