import { createBrowserRouter } from 'react-router-dom'
import { Home } from '../features/home/Home'
import { VerifyPage } from '../features/auth/VerifyPage'
import { AdminDashboard } from '../features/admin/AdminDashboard'

// The screen map fills in per milestone (backbone §9.3). M1 adds the auth flow:
// the root branches on session state, and the emailed link lands on /auth/verify.
export const router = createBrowserRouter([
  {
    path: '/',
    element: <Home />,
  },
  {
    path: '/received',
    element: <Home />,
  },
  {
    path: '/me/fingerprint',
    element: <Home />,
  },
  {
    path: '/profile',
    element: <Home />,
  },
  {
    path: '/settings',
    element: <Home />,
  },
  {
    path: '/you',
    element: <Home />,
  },
  {
    path: '/auth/verify',
    element: <VerifyPage />,
  },
  {
    path: '/admin',
    element: <AdminDashboard />,
  },
])
