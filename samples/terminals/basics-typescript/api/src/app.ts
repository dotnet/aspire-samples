import express, { type ErrorRequestHandler, type Response } from 'express';

export interface NotesDatabase {
    hGetAll(key: string): Promise<Record<string, string>>;
    hGet(key: string, field: string): Promise<string | null | undefined>;
    hSet(key: string, field: string, value: string): Promise<number>;
    hDel(key: string, field: string): Promise<number>;
    ping(): Promise<string>;
}

function problem(response: Response, status: number, title: string) {
    return response.status(status).type('application/problem+json').json({
        type: 'about:blank', title, status,
    });
}

class RedisRequestError extends Error {}

async function query<T>(operation: Promise<T>): Promise<T> {
    try {
        return await operation;
    } catch (cause) {
        throw new RedisRequestError('Redis request failed', { cause });
    }
}

export function createApp(redis: NotesDatabase) {
    const app = express();
    app.use(express.json({ limit: '10kb' }));
    app.get('/health', async (_request, response) => {
        await query(redis.ping());
        response.json({ status: 'healthy' });
    });
    app.param('id', (_request, response, next, id: string) => {
        if (!/^[a-z0-9-]{1,40}$/.test(id)) {
            problem(response, 400, 'Use 1-40 lowercase letters, digits or hyphens for the note ID');
            return;
        }
        next();
    });
    // Demo-only, unauthenticated CRUD routes: every caller can read, replace, and delete notes.
    // Use non-sensitive, disposable data and keep this HTTP API private.
    app.get('/notes', async (_request, response) => {
        const notes = await query(redis.hGetAll('notes'));
        response.json(Object.keys(notes).sort().map(id => ({ id, text: notes[id] })));
    });
    app.get('/notes/:id', async (request, response) => {
        const id = request.params.id;
        const text = await query(redis.hGet('notes', id));
        if (text == null) {
            problem(response, 404, 'Note not found');
            return;
        }
        response.json({ id, text });
    });
    app.put('/notes/:id', async (request, response) => {
        if (!request.is('application/json')) {
            problem(response, 415, 'Use application/json');
            return;
        }
        const text: unknown = request.body?.text;
        if (typeof text !== 'string' || !text.trim() || text.length > 1000) {
            problem(response, 400, 'Supply text containing 1-1000 characters, not just whitespace');
            return;
        }
        const id = request.params.id;
        const created = await query(redis.hSet('notes', id, text));
        if (created) {
            response.location(`/notes/${id}`);
        }
        response.status(created ? 201 : 200).json({ id, text });
    });
    app.delete('/notes/:id', async (request, response) => {
        if (!await query(redis.hDel('notes', request.params.id))) {
            problem(response, 404, 'Note not found');
            return;
        }
        response.sendStatus(204);
    });
    const handleError: ErrorRequestHandler = (error: unknown, _request, response, _next) => {
        console.error('Request failed:', error);
        if (error && typeof error === 'object' && 'type' in error) {
            if (error.type === 'entity.parse.failed') {
                problem(response, 400, 'Invalid JSON');
                return;
            }
            if (error.type === 'entity.too.large') {
                problem(response, 413, 'Request body too large');
                return;
            }
        }
        if (error instanceof RedisRequestError) {
            problem(response, 503, 'Redis is unavailable');
            return;
        }
        problem(response, 500, 'An unexpected error occurred');
    };
    app.use(handleError);
    return app;
}
