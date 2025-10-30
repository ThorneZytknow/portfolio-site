// Nicolas — Portfolio Interactions
(function () {
  const doc = document.documentElement;
  const header = document.querySelector('.site-header');
  const progress = document.querySelector('.scroll-progress');
  const nav = document.querySelector('.site-nav');
  const navToggle = document.querySelector('.nav-toggle');
  const themeToggle = document.querySelector('.theme-toggle');
  const prefersDark = window.matchMedia('(prefers-color-scheme: dark)');
  // Fallback para imagens ausentes
  const fallbackImg = (img) => {
    if (img && !img.dataset.fallback) {
      img.dataset.fallback = '1';
      // Inline SVG data URI (teal on indigo) para evitar dependência de arquivo local
      const svg = encodeURIComponent("<svg xmlns='http://www.w3.org/2000/svg' width='1600' height='900' viewBox='0 0 1600 900'><defs><linearGradient id='g' x1='0' x2='1' y1='0' y2='1'><stop offset='0%' stop-color='#11151a'/><stop offset='100%' stop-color='#1b1f3b'/></linearGradient></defs><rect width='1600' height='900' fill='url(#g)'/><rect x='40' y='40' width='1520' height='820' rx='24' ry='24' fill='none' stroke='%2319d3d3' stroke-opacity='.5'/><text x='800' y='460' text-anchor='middle' font-size='42' font-family='Segoe UI, Arial' fill='%23aeb7bd'>Imagem</text><text x='800' y='510' text-anchor='middle' font-size='20' font-family='Segoe UI, Arial' fill='%236f7980'>placeholder</text></svg>");
      img.src = `data:image/svg+xml;charset=UTF-8,${svg}`;
    }
  };
  window.addEventListener('error', (e) => {
    const el = e.target;
    if (el && el.tagName === 'IMG') fallbackImg(el);
  }, true);
  document.querySelectorAll('img').forEach((img) => {
    img.addEventListener('error', () => fallbackImg(img));
  });

  // Sticky header subtle state on scroll
  const onScroll = () => {
    if (window.scrollY > 10) header.classList.add('scrolled');
    else header.classList.remove('scrolled');
    // progress bar
    const max = document.documentElement.scrollHeight - window.innerHeight;
    const pct = Math.max(0, Math.min(1, (window.scrollY || 0) / (max || 1)));
    if (progress) progress.style.width = `${pct * 100}%`;
  };
  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();

  // Toggle de navegação (mobile)
  navToggle?.addEventListener('click', () => {
    const expanded = nav.getAttribute('aria-expanded') === 'true';
    nav.setAttribute('aria-expanded', String(!expanded));
    navToggle.setAttribute('aria-expanded', String(!expanded));
  });
  // Fecha o menu ao clicar em um link (mobile)
  nav?.querySelectorAll('a').forEach((a) => a.addEventListener('click', () => {
    nav.setAttribute('aria-expanded', 'false');
    navToggle?.setAttribute('aria-expanded', 'false');
  }));

  // Alternância de tema com persistência
  const STORAGE_KEY = 'nicolas-theme';
  const applyTheme = (mode) => {
    if (mode === 'system') {
      doc.setAttribute('data-theme', prefersDark.matches ? 'dark' : 'light');
    } else {
      doc.setAttribute('data-theme', mode);
    }
  };
  const saved = localStorage.getItem(STORAGE_KEY);
  applyTheme(saved || 'dark');
  themeToggle?.addEventListener('click', () => {
    const current = doc.getAttribute('data-theme');
    const next = current === 'dark' ? 'light' : 'dark';
    localStorage.setItem(STORAGE_KEY, next);
    applyTheme(next);
  });
  prefersDark.addEventListener('change', () => {
    const mode = localStorage.getItem(STORAGE_KEY) || 'system';
    if (mode === 'system') applyTheme('system');
  });

  // Reveal on scroll
  const io = new IntersectionObserver((entries) => {
    for (const e of entries) {
      if (e.isIntersecting) {
        e.target.classList.add('in-view');
        io.unobserve(e.target);
      }
    }
  }, { threshold: 0.1 });
  document.querySelectorAll('.reveal').forEach((el) => io.observe(el));

  // Conteúdo do modal de projetos (demo)
  const modal = document.getElementById('project-modal');
  const modalMedia = modal?.querySelector('.modal-media');
  const modalContent = modal?.querySelector('.modal-content');
  const closeEls = modal?.querySelectorAll('[data-close]');
  const projects = {
    p1: {
      media: '<img src="assets/project-api.jpg" alt="API FastAPI e Postgres">',
      title: 'API de Tarefas — FastAPI + Postgres',
      body: 'CRUD de tarefas com autenticação JWT, testes (Pytest), migrações (Alembic) e containerização com Docker. Observabilidade com Prometheus e logs estruturados.',
      repo: 'https://github.com/usuario/api-tarefas',
      live: ''
    },
    p2: {
      media: '<img src="assets/project-ecommerce.jpg" alt="Loja headless com Next e Stripe">',
      title: 'Ecommerce Headless — Next.js + Stripe',
      body: 'SSR/ISR para catálogo, carrinho persistente, checkout com Stripe e webhooks para confirmação de pedido. CMS opcional via Markdown/MDX.',
      repo: 'https://github.com/usuario/next-stripe-store',
      live: 'https://demo-store.example.com'
    },
    p3: {
      media: '<img src="assets/project-chat.jpg" alt="Chat em tempo real">',
      title: 'Chat Tempo Real — Node + WebSocket',
      body: 'Salas, presença, tipagem de eventos com TypeScript, rate limiting e persistência de mensagens no Redis. Testes end‑to‑end com Playwright.',
      repo: 'https://github.com/usuario/node-realtime-chat',
      live: ''
    },
    p4: {
      media: '<img src="assets/project-dashboard.jpg" alt="Dashboard analytics">',
      title: 'Dashboard Analytics — React + D3',
      body: 'Conexão a API, filtros por período, gráficos customizados com D3 e acessibilidade em foco (teclado e ARIA).',
      repo: 'https://github.com/usuario/analytics-dashboard',
      live: ''
    },
    p5: {
      media: '<img src="assets/project-microservice.jpg" alt="Microserviço Go">',
      title: 'Microserviço de Imagens — Go + gRPC',
      body: 'Upload, transformação (resize/crop) e armazenamento em MinIO, comunicação gRPC e gateway HTTP. CI para build e testes.',
      repo: 'https://github.com/usuario/go-image-service',
      live: ''
    },
    p6: {
      media: '<img src="assets/project-cicd.jpg" alt="CI/CD com Actions e Terraform">',
      title: 'Infra & CI/CD — Actions + Terraform',
      body: 'Pipelines com lint, testes, build e deploy. Infra como código para provisionar ambiente em AWS (S3/EC2) com Terraform.',
      repo: 'https://github.com/usuario/iac-cicd-template',
      live: ''
    }
  };

  const openModal = (id) => {
    const data = projects[id];
    if (!data || !modal) return;
    modalMedia.innerHTML = data.media;
    const links = `
      <div class="modal-actions">
        ${data.repo ? `<a class="btn btn-ghost" href="${data.repo}" target="_blank" rel="noreferrer noopener">Código</a>` : ''}
        ${data.live ? `<a class="btn btn-primary" href="${data.live}" target="_blank" rel="noreferrer noopener">Demo</a>` : ''}
      </div>`;
    modalContent.innerHTML = `<h3>${data.title}</h3><p>${data.body}</p>${links}`;
    modal.setAttribute('open', '');
    modal.setAttribute('aria-hidden', 'false');
    document.body.style.overflow = 'hidden';
  };
  const closeModal = () => {
    if (!modal) return;
    modal.removeAttribute('open');
    modal.setAttribute('aria-hidden', 'true');
    document.body.style.overflow = '';
  };
  closeEls?.forEach((el) => el.addEventListener('click', closeModal));
  modal?.addEventListener('click', (e) => {
    if (e.target === modal || e.target.dataset.close !== undefined) closeModal();
  });
  document.addEventListener('keydown', (e) => { if (e.key === 'Escape') closeModal(); });

  document.querySelectorAll('.project-card').forEach((card) => {
    const id = card.getAttribute('data-project');
    const handler = () => openModal(id);
    card.addEventListener('click', handler);
    card.addEventListener('keypress', (e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); handler(); } });
    // tilt effect
    card.addEventListener('mousemove', (e) => {
      const r = card.getBoundingClientRect();
      const x = e.clientX - r.left; const y = e.clientY - r.top;
      const rx = ((y / r.height) - 0.5) * -6; // rotateX
      const ry = ((x / r.width) - 0.5) * 6;   // rotateY
      card.style.transform = `rotateX(${rx}deg) rotateY(${ry}deg)`;
    });
    card.addEventListener('mouseleave', () => { card.style.transform = ''; });
    const vid = card.querySelector('.card-video');
    if (vid) {
      vid.muted = true; vid.playsInline = true; vid.autoplay = true; vid.loop = true;
      const onCanPlay = () => { card.classList.add('has-video'); vid.play().catch(()=>{}); };
      vid.addEventListener('canplay', onCanPlay, { once: true });
      vid.addEventListener('error', () => { vid.remove(); });
    }
  });

  // Formulário de contato (apenas demo no cliente)
  const form = document.querySelector('.contact-form');
  const status = form?.querySelector('.form-status');
  form?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const fd = new FormData(form);
    const name = (fd.get('name') || '').toString().trim();
    const email = (fd.get('email') || '').toString().trim();
    const message = (fd.get('message') || '').toString().trim();
    if (!name || !email || !message) {
      status.textContent = 'Preencha todos os campos.';
      return;
    }
    // Simula envio assíncrono; depois troque pelo seu endpoint (ex.: Formspree)
    status.textContent = 'Enviando…';
    await new Promise((r) => setTimeout(r, 800));
    status.textContent = 'Valeu — retorno em breve!';
    form.reset();
  });

  // Cursor criativo para grid de projetos
  const grid = document.querySelector('.projects-grid');
  const setPos = (e) => {
    document.body.style.setProperty('--mx', e.clientX + 'px');
    document.body.style.setProperty('--my', e.clientY + 'px');
  };
  grid?.addEventListener('mouseenter', () => { document.body.dataset.cursor = 'projects'; });
  grid?.addEventListener('mousemove', setPos);
  grid?.addEventListener('mouseleave', () => { document.body.dataset.cursor = ''; });

  // Parallax leve no hero
  const hero = document.querySelector('#hero');
  const bg = hero?.querySelector('.bg-video');
  const inner = hero?.querySelector('.hero-inner');
  // Se o vídeo falhar, mantém poster/placeholder e remove elemento para evitar visual preto
  bg?.addEventListener('error', () => { bg.style.display = 'none'; });
  hero?.addEventListener('mousemove', (e) => {
    const r = hero.getBoundingClientRect();
    const px = (e.clientX - r.left) / r.width - 0.5;
    const py = (e.clientY - r.top) / r.height - 0.5;
    bg && (bg.style.transform = `translate(${px * 10}px, ${py * 10}px) scale(1.02)`);
    inner && (inner.style.transform = `translate(${px * -6}px, ${py * -6}px)`);
  });
  hero?.addEventListener('mouseleave', () => {
    bg && (bg.style.transform = '');
    inner && (inner.style.transform = '');
  });

  // Contagem animada de métricas
  const metricIO = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (!entry.isIntersecting) return;
      const el = entry.target.querySelector('.value');
      if (!el || el.dataset.done) return;
      el.dataset.done = '1';
      const target = parseFloat(el.dataset.target || '0');
      const decimals = parseInt(el.dataset.decimals || '0', 10);
      const suffix = el.dataset.suffix || '';
      const start = performance.now();
      const dur = 1000;
      const tick = (t) => {
        const k = Math.min(1, (t - start) / dur);
        const val = (target * k).toFixed(decimals);
        el.textContent = val + suffix;
        if (k < 1) requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
      metricIO.unobserve(entry.target);
    });
  }, { threshold: 0.4 });
  document.querySelectorAll('.metric').forEach((m) => metricIO.observe(m));
})();
