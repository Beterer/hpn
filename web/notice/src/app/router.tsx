import { createBrowserRouter } from 'react-router-dom'
import { LandingPage } from '../features/landing/LandingPage'

// The screen map fills in per milestone (backbone §9.3). M0 ships the landing route.
export const router = createBrowserRouter([
  {
    path: '/',
    element: <LandingPage />,
  },
])
