import React, { useEffect, useState, useCallback } from 'react';
import { postsApi, commentsApi } from '../api/client';
import { useAuth } from '../contexts/AuthContext';
import type { Post, Comment, PostsPagedResponse, ApiError } from '../types';

// ── Huvudsidan med inläggslista ─────────────────────────────────────────────

export function PostsPage() {
  const { user } = useAuth();
  const [data,     setData]     = useState<PostsPagedResponse | null>(null);
  const [loading,  setLoading]  = useState(true);
  const [error,    setError]    = useState('');
  const [editId,   setEditId]   = useState<number | null>(null);
  const [openPost, setOpenPost] = useState<number | null>(null); // vilket inlägg som visar kommentarer
  const [search,   setSearch]   = useState('');
  const [page,     setPage]     = useState(1);

  const fetchPosts = useCallback(async (p = page, s = search) => {
    setLoading(true);
    setError('');
    try {
      setData(await postsApi.getAll({ page: p, pageSize: 8, search: s || undefined }));
    } catch (e) { setError((e as ApiError).message); }
    finally { setLoading(false); }
  }, [page, search]);

  useEffect(() => { fetchPosts(); }, [fetchPosts]);

  async function handleDelete(id: number) {
    if (!confirm('Delete this post?')) return;
    try { await postsApi.delete(id); fetchPosts(); }
    catch (e) { alert((e as ApiError).message); }
  }

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    setPage(1);
    fetchPosts(1, search);
  }

  return (
    <div className="posts-page">
      {/* Sökfält */}
      <form onSubmit={handleSearch} className="search-bar">
        <input placeholder="Search posts…" value={search}
          onChange={e => setSearch(e.target.value)} />
        <button type="submit" className="btn btn-secondary btn-sm">Search</button>
        {search && (
          <button type="button" className="btn btn-secondary btn-sm"
            onClick={() => { setSearch(''); setPage(1); fetchPosts(1, ''); }}>
            Clear
          </button>
        )}
      </form>

      {/* Skapa inlägg – bara synligt om man är inloggad */}
      {user && <CreatePostForm onCreated={() => fetchPosts(1)} />}

      {error && <p className="error-msg">{error}</p>}

      {loading
        ? <div className="loading">Loading posts…</div>
        : data?.items.length === 0
          ? <p className="empty-state">No posts yet. Be the first!</p>
          : data?.items.map(post =>
              editId === post.id
                ? <EditPostForm key={post.id} post={post}
                    onSaved={() => { setEditId(null); fetchPosts(); }}
                    onCancel={() => setEditId(null)} />
                : <PostCard key={post.id} post={post}
                    expanded={openPost === post.id}
                    onToggle={() => setOpenPost(openPost === post.id ? null : post.id)}
                    canModify={!!user && (user.username === post.authorUsername || user.role === 'Admin')}
                    onEdit={() => setEditId(post.id)}
                    onDelete={() => handleDelete(post.id)} />
            )
      }

      {/* Sidnumrering */}
      {data && data.totalPages > 1 && (
        <div className="pagination">
          <button className="btn btn-secondary btn-sm"
            disabled={page <= 1}
            onClick={() => { setPage(p => p - 1); fetchPosts(page - 1); }}>
            ← Prev
          </button>
          <span>{page} / {data.totalPages}  ({data.totalCount} posts)</span>
          <button className="btn btn-secondary btn-sm"
            disabled={page >= data.totalPages}
            onClick={() => { setPage(p => p + 1); fetchPosts(page + 1); }}>
            Next →
          </button>
        </div>
      )}
    </div>
  );
}

// ── Formulär för nytt inlägg ────────────────────────────────────────────────

function CreatePostForm({ onCreated }: { onCreated: () => void }) {
  const [title,   setTitle]   = useState('');
  const [content, setContent] = useState('');
  const [image,   setImage]   = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error,   setError]   = useState('');

  function handleImageChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size > 5_242_880) { setError('Image must be under 5 MB.'); return; }
    setImage(file);
    setPreview(URL.createObjectURL(file)); // snabb förhandsvisning utan uppladdning
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    if (title.trim().length < 3)   { setError('Title: min 3 chars.'); return; }
    if (content.trim().length < 1) { setError('Content is required.'); return; }
    setLoading(true);
    try {
      await postsApi.create({ title: title.trim(), content: content.trim() }, image ?? undefined);
      setTitle(''); setContent(''); setImage(null); setPreview(null);
      onCreated();
    } catch (e) { setError((e as ApiError).message); }
    finally { setLoading(false); }
  }

  return (
    <div className="card create-post">
      <h2>New Post</h2>
      <form onSubmit={handleSubmit} noValidate>
        <div className="field">
          <label htmlFor="new-title">Title</label>
          <input id="new-title" value={title} required onChange={e => setTitle(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="new-content">Content</label>
          <textarea id="new-content" rows={4} value={content} required
            onChange={e => setContent(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="new-image">Image (optional, JPG/PNG max 5 MB)</label>
          <input id="new-image" type="file" accept=".jpg,.jpeg,.png,.webp"
            onChange={handleImageChange} />
          {preview && <img src={preview} alt="preview" className="img-preview" />}
        </div>
        {error && <p className="error-msg" role="alert">{error}</p>}
        <button type="submit" disabled={loading} className="btn btn-primary">
          {loading ? 'Publishing…' : 'Publish'}
        </button>
      </form>
    </div>
  );
}

// ── Formulär för att redigera inlägg ────────────────────────────────────────

function EditPostForm({ post, onSaved, onCancel }:
  { post: Post; onSaved: () => void; onCancel: () => void }) {
  const [title,   setTitle]   = useState(post.title);
  const [content, setContent] = useState(post.content);
  const [loading, setLoading] = useState(false);
  const [error,   setError]   = useState('');

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    if (title.trim().length < 3) { setError('Title: min 3 chars.'); return; }
    setLoading(true);
    try { await postsApi.update(post.id, { title: title.trim(), content: content.trim() }); onSaved(); }
    catch (e) { setError((e as ApiError).message); }
    finally { setLoading(false); }
  }

  return (
    <div className="card edit-post">
      <h2>Edit Post</h2>
      <form onSubmit={handleSubmit} noValidate>
        <div className="field">
          <label>Title</label>
          <input value={title} required onChange={e => setTitle(e.target.value)} />
        </div>
        <div className="field">
          <label>Content</label>
          <textarea rows={4} value={content} required onChange={e => setContent(e.target.value)} />
        </div>
        {error && <p className="error-msg">{error}</p>}
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

// ── Inläggskort med kommentarer ─────────────────────────────────────────────

interface PostCardProps {
  post: Post; expanded: boolean; canModify: boolean;
  onToggle: () => void; onEdit: () => void; onDelete: () => void;
}

function PostCard({ post, expanded, canModify, onToggle, onEdit, onDelete }: PostCardProps) {
  const [comments, setComments] = useState<Comment[]>([]);
  const [loadingC, setLoadingC] = useState(false);
  const [newComment, setNewComment] = useState('');
  const [savingC,  setSavingC]  = useState(false);
  const { user } = useAuth();

  // Hämta kommentarer först när användaren expanderar (lazy load)
  useEffect(() => {
    if (!expanded) return;
    setLoadingC(true);
    commentsApi.getByPost(post.id)
      .then(setComments)
      .finally(() => setLoadingC(false));
  }, [expanded, post.id]);

  async function submitComment(e: React.FormEvent) {
    e.preventDefault();
    if (!newComment.trim()) return;
    setSavingC(true);
    try {
      const c = await commentsApi.create(post.id, { content: newComment.trim() });
      setComments(prev => [...prev, c]);
      setNewComment('');
    } finally { setSavingC(false); }
  }

  async function deleteComment(commentId: number) {
    await commentsApi.delete(post.id, commentId);
    setComments(prev => prev.filter(c => c.id !== commentId));
  }

  const date = new Date(post.createdAt).toLocaleDateString('en-GB',
    { year: 'numeric', month: 'short', day: 'numeric' });

  return (
    <article className="card post-card">
      {post.imageUrl && (
        <img src={`http://localhost:5000${post.imageUrl}`} alt={post.title} className="post-image" />
      )}
      <header className="post-header">
        <h2>{post.title}</h2>
        <div className="post-meta">
          <span className="author">✍ {post.authorUsername}</span>
          <time>{date}</time>
          <span>👁 {post.viewCount}</span>
          <span>💬 {post.commentCount}</span>
        </div>
      </header>
      <p className="post-content">{post.content}</p>

      <footer className="post-actions">
        <button className="btn btn-secondary btn-sm" onClick={onToggle}>
          {expanded ? 'Hide comments' : `Comments (${post.commentCount})`}
        </button>
        {canModify && <>
          <button className="btn btn-secondary btn-sm" onClick={onEdit}>Edit</button>
          <button className="btn btn-danger btn-sm"    onClick={onDelete}>Delete</button>
        </>}
      </footer>

      {expanded && (
        <div className="comments-section">
          {loadingC ? <p className="loading">Loading…</p> : comments.map(c => (
            <div key={c.id} className="comment">
              <div className="comment-meta">
                <span className="author">{c.authorUsername}</span>
                <time>{new Date(c.createdAt).toLocaleDateString()}</time>
                {/* Radera-knapp bara för egen kommentar eller admin */}
                {user && (user.username === c.authorUsername || user.role === 'Admin') && (
                  <button className="btn btn-danger btn-xs" onClick={() => deleteComment(c.id)}>×</button>
                )}
              </div>
              <p>{c.content}</p>
            </div>
          ))}
          {user && (
            <form onSubmit={submitComment} className="comment-form">
              <textarea placeholder="Write a comment…" rows={2} value={newComment}
                onChange={e => setNewComment(e.target.value)} required />
              <button type="submit" disabled={savingC} className="btn btn-primary btn-sm">
                {savingC ? '…' : 'Post'}
              </button>
            </form>
          )}
        </div>
      )}
    </article>
  );
}
