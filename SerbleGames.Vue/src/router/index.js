import { createRouter, createWebHistory } from 'vue-router'
import StoreView from '../views/StoreView.vue'

const routes = [
  {
    path: '/',
    name: 'store',
    component: StoreView
  },
  {
    path: '/game/:id',
    name: 'game-detail',
    component: () => import('../views/GameDetailView.vue')
  },
  {
    path: '/library',
    name: 'library',
    component: () => import('../views/LibraryView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/creator',
    name: 'creator',
    component: () => import('../views/CreatorPortalView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/profile/:id',
    name: 'profile',
    component: () => import('../views/ProfileView.vue')
  },
  {
    path: '/account',
    name: 'account',
    component: () => import('../views/AccountView.vue'),
    meta: { requiresAuth: true }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const isAuthenticated = !!localStorage.getItem('backend_token')
  if (to.meta.requiresAuth && !isAuthenticated) {
    next({ name: 'store' })
  } else {
    next()
  }
})

export default router
