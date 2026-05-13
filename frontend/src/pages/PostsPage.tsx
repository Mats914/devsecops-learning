import React, { useEffect, useState, useCallback } from 'react';
import { postsApi } from '../api/client';
import { useAuth } from '../contexts/AuthContext';
import type { Post, ApiError } from '../types';

export function PostsPage() {
  const { user } = useAuth();
  const [posts,   setPosts]   = useState<Post[]>([]);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState('');
  const [editId,  setEditId]  = useState<number | null>(null);

  const fetchPosts = useCallback(async () => {
    try {
      const data = await postsApi.getAll();
      setPosts(data);
    } catch (err) {
      setError((err as ApiError).message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchPosts(); }, [fetchPosts]);

  async function handleDelete(id: number) {
    if (!confirm('Delete this post?')) return;
    try {
      await postsApi.delete(id);
      setPosts(prev => prev.filter(p => p.id !== id));
    } catch (err) {
      alert((err as ApiError).message);
    }
  }

  if (loading) return <div className="loading">Loading posts…</div>;

  return (
    <div className="posts-page">
      {user && <CreatePostForm onCreated={fetchPosts} />}

      {error && <p className="error-msg">{error}</p>}

      {posts.length === 0
        ? <p className="empty-state">No posts yet. Be the first to write one!</p>
        : posts.map(post =>
            editId === post.id
              ? <EditPostForm
                  key={post.id}
                  post={post}
                  onSaved={() => { setEditId(null); fetchPosts(); }}
                  onCancel={() => setEditId(null)}
                />
              : <PostCard
                  key={post.id}
                  post={post}
                  canModify={
                    !!user &&
                    (user.username === post.authorUsername || user.role === 'Admin')
                  }
                  onEdit={() => setEditId(post.id)}
                  onDelete={() => handleDelete(post.id)}
                />
          )
      }
    </div>
  );
}

// ── Create form ────────────────────────────────────────────────────────────

function CreatePostForm({ onCreated }: { onCreated: () => void }) {
  const [title,   setTitle]   = useState('');
  const [content, setContent] = useState('');
  const [loading, setLoading] = useState(false);
  const [error,   setError]   = useState('');

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');

    // Client-side validation (backend also validates)
    if (title.trim().length < 3)   { setError('Title must be at least 3 characters.'); return; }
    if (content.trim().length < 1) { setError('Content cannot be empty.'); return; }

    setLoading(true);
    try {
      await postsApi.create({ title: title.trim(), content: content.trim() });
      setTitle('');
      setContent('');
      onCreated();
    } catch (err) {
      setError((err as ApiError).message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="card create-post">
      <h2>New Post</h2>
      <form onSubmit={handleSubmit} noValidate>
        <div className="field">
          <label htmlFor="new-title">Title</label>
          <input id="new-title" value={title} onChange={e => setTitle(e.target.value)} required />
        </div>
        <div className="field">
          <label htmlFor="new-content">Content</label>
          <textarea id="new-content" rows={4} value={content}
            onChange={e => setContent(e.target.value)} required />
        </div>
        {error && <p className="error-msg" role="alert">{error}</p>}
        <button type="submit" disabled={loading} className="btn btn-primary">
          {loading ? 'Publishing…' : 'Publish'}
        </button>
      </form>
    </div>
  );
}

// ── Edit form ──────────────────────────────────────────────────────────────

function EditPostForm({
  post, onSaved, onCancel,
}: { post: Post; onSaved: () => void; onCancel: () => void }) {
  const [title,   setTitle]   = useState(post.title);
  const [content, setContent] = useState(post.content);
  const [loading, setLoading] = useState(false);
  const [error,   setError]   = useState('');

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');

    if (title.trim().length < 3)   { setError('Title must be at least 3 characters.'); return; }
    if (content.trim().length < 1) { setError('Content cannot be empty.'); return; }

    setLoading(true);
    try {
      await postsApi.update(post.id, { title: title.trim(), content: content.trim() });
      onSaved();
    } catch (err) {
      setError((err as ApiError).message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="card edit-post">
      <h2>Edit Post</h2>
      <form onSubmit={handleSubmit} noValidate>
        <div className="field">
          <label htmlFor="edit-title">Title</label>
          <input id="edit-title" value={title} onChange={e => setTitle(e.target.value)} required />
        </div>
        <div className="field">
          <label htmlFor="edit-content">Content</label>
          <textarea id="edit-content" rows={4} value={content}
            onChange={e => setContent(e.target.value)} required />
        </div>
        {error && <p className="error-msg" role="alert">{error}</p>}
        <div className="btn-row">
          <button type="submit" disabled={loading} className="btn btn-primary">
            {loading ? 'Saving…' : 'Save'}
          </button>
          <button type="button" onClick={onCancel} className="btn btn-secondary">Cancel</button>
        </div>
      </form>
    </div>
  );
}

// ── Post card ──────────────────────────────────────────────────────────────

interface PostCardProps {
  post: Post;
  canModify: boolean;
  onEdit: () => void;
  onDelete: () => void;
}

function PostCard({ post, canModify, onEdit, onDelete }: PostCardProps) {
  const date = new Date(post.createdAt).toLocaleDateString('sv-SE', {
    year: 'numeric', month: 'short', day: 'numeric',
  });

  return (
    <article className="card post-card">
      <header className="post-header">
        <h2>{post.title}</h2>
        <div className="post-meta">
          <span className="author">✍ {post.authorUsername}</span>
          <time>{date}</time>
        </div>
      </header>
      <p className="post-content">{post.content}</p>
      {canModify && (
        <footer className="post-actions">
          <button className="btn btn-secondary btn-sm" onClick={onEdit}>Edit</button>
          <button className="btn btn-danger btn-sm"    onClick={onDelete}>Delete</button>
        </footer>
      )}
    </article>
  );
}
