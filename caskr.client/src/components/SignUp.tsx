import {
  useState,
  useEffect,
  useRef,
  type ChangeEvent,
  type FocusEvent,
  type FormEvent,
  type KeyboardEvent,
} from 'react';
import { Eye, EyeOff, Lock, User, Mail, Building2, AlertCircle, Loader2, CheckCircle2 } from 'lucide-react';

type Company = {
  id: number;
  name: string;
};

type FormDataState = {
  name: string;
  email: string;
  password: string;
  confirmPassword: string;
  companyName: string;
  existingCompanyId: number | null;
  userTypeId: number;
};

type FormField = 'name' | 'email' | 'password' | 'confirmPassword' | 'companyName';

const isFormField = (value: string): value is FormField =>
  value === 'name' ||
  value === 'email' ||
  value === 'password' ||
  value === 'confirmPassword' ||
  value === 'companyName';

export default function SignUp() {
  const [step, setStep] = useState<'form' | 'success'>('form');
  const [formData, setFormData] = useState<FormDataState>({
    name: '',
    email: '',
    password: '',
    confirmPassword: '',
    companyName: '',
    existingCompanyId: null,
    userTypeId: 1 // Default user type, adjust based on your system
  });
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [touched, setTouched] = useState<Record<string, boolean>>({});
  const [focusedField, setFocusedField] = useState<string | null>(null);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [isNewCompany, setIsNewCompany] = useState(true);
  const nameRef = useRef<HTMLInputElement | null>(null);

  // Focus name field on mount
  useEffect(() => {
    nameRef.current?.focus();
  }, []);

  // Placeholder for fetching available companies when needed
  useEffect(() => {
    const fetchCompanies = async () => {
      try {
        // Real implementation would retrieve company options for admin flows.
        setCompanies(prev => prev);
      } catch (error) {
        console.error('Failed to fetch companies:', error);
      }
    };

    void fetchCompanies();
  }, []);

  // Comprehensive validation rules
  const validateField = (name: FormField, value: string) => {
    switch (name) {
      case 'name':
        if (!value.trim()) {
          return 'Full name is required';
        }
        if (value.trim().length < 2) {
          return 'Name must be at least 2 characters';
        }
        if (!/^[a-zA-Z\s'-]+$/.test(value)) {
          return 'Name can only contain letters, spaces, hyphens, and apostrophes';
        }
        return '';
      
      case 'email':
        if (!value.trim()) {
          return 'Email is required';
        }
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
          return 'Please enter a valid email address';
        }
        return '';
      
      case 'password':
        if (!value) {
          return 'Password is required';
        }
        if (value.length < 8) {
          return 'Password must be at least 8 characters';
        }
        if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(value)) {
          return 'Password must contain uppercase, lowercase, and number';
        }
        return '';
      
      case 'confirmPassword':
        if (!value) {
          return 'Please confirm your password';
        }
        if (value !== formData.password) {
          return 'Passwords do not match';
        }
        return '';
      
      case 'companyName':
        if (isNewCompany && !value.trim()) {
          return 'Company name is required';
        }
        if (isNewCompany && value.trim().length < 2) {
          return 'Company name must be at least 2 characters';
        }
        return '';
      
      default:
        return '';
    }
  };

  // Real-time validation on change
  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;

    if (name === 'existingCompanyId') {
      setFormData(prev => ({
        ...prev,
        existingCompanyId: value ? parseInt(value) : null
      }));
    } else if (isFormField(name)) {
      setFormData(prev => ({ ...prev, [name]: value }));
    }

    // Clear error for this field when user starts typing
    if (touched[name] && isFormField(name)) {
      const error = validateField(name, value);
      setErrors(prev => ({ ...prev, [name]: error }));

      // Also validate confirmPassword when password changes
      if (name === 'password' && touched.confirmPassword) {
        const confirmError = validateField('confirmPassword', formData.confirmPassword);
        setErrors(prev => ({ ...prev, confirmPassword: confirmError }));
      }
    }
  };

  // Mark field as touched on blur
  const handleBlur = (e: FocusEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setTouched(prev => ({ ...prev, [name]: true }));
    if (isFormField(name)) {
      const error = validateField(name, value);
      setErrors(prev => ({ ...prev, [name]: error }));
    }
    setFocusedField(null);
  };

  const handleFocus = (fieldName: string) => {
    setFocusedField(fieldName);
  };

  const submitForm = async () => {
    const fieldsToValidate: FormField[] = ['name', 'email', 'password', 'confirmPassword'];
    if (isNewCompany) {
      fieldsToValidate.push('companyName');
    }

    const newTouched: Record<string, boolean> = {};
    fieldsToValidate.forEach(field => {
      newTouched[field] = true;
    });
    setTouched(newTouched);

    const newErrors: Record<string, string> = {};
    fieldsToValidate.forEach(field => {
      const value = formData[field];
      const error = validateField(field, value);
      if (error) {
        newErrors[field] = error;
      }
    });

    setErrors(newErrors);

    if (Object.keys(newErrors).length > 0) {
      const firstErrorField = fieldsToValidate.find(field => newErrors[field]);
      if (firstErrorField === 'name') {
        nameRef.current?.focus();
      }
      return;
    }

    setIsLoading(true);

    try {
      const registrationData = {
        name: formData.name,
        email: formData.email,
        password: formData.password,
        companyName: isNewCompany ? formData.companyName : undefined,
        companyId: !isNewCompany ? formData.existingCompanyId : undefined,
        userTypeId: formData.userTypeId
      };

      const response = await fetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(registrationData)
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || 'Registration failed');
      }

      const data = await response.json();
      console.log('Registration successful:', data);

      setStep('success');

      // Optional: Auto-login after registration
      // const loginResponse = await fetch('/api/auth/login', {
      //   method: 'POST',
      //   headers: { 'Content-Type': 'application/json' },
      //   body: JSON.stringify({
      //     email: formData.email,
      //     password: formData.password
      //   })
      // });
      //
      // if (loginResponse.ok) {
      //   const loginData = await loginResponse.json();
      //   localStorage.setItem('token', loginData.token);
      //   window.location.href = '/';
      // }

    } catch (error) {
      console.error('Registration error:', error);
      setErrors({
        email: error instanceof Error ? error.message : 'Registration failed. Please try again.'
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    await submitForm();
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement | HTMLSelectElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      void submitForm();
    }
  };

  const isFormValid = 
    formData.name && 
    formData.email && 
    formData.password && 
    formData.confirmPassword &&
    (isNewCompany ? formData.companyName : formData.existingCompanyId) &&
    Object.keys(errors).length === 0;

  if (step === 'success') {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 flex items-center justify-center p-4">
        {/* Subtle animated background */}
        <div className="absolute inset-0 overflow-hidden pointer-events-none">
          <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-blue-500/5 rounded-full blur-3xl animate-pulse"></div>
          <div className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-purple-500/5 rounded-full blur-3xl animate-pulse" style={{ animationDelay: '1s' }}></div>
        </div>

        {/* Success Card */}
        <div className="relative w-full max-w-md">
          <div className="absolute inset-0 bg-gradient-to-r from-green-500/20 to-blue-500/20 rounded-2xl blur-xl"></div>
          
          <div className="relative bg-slate-800/90 backdrop-blur-xl rounded-2xl shadow-2xl border border-slate-700/50 p-8 text-center">
            <div className="inline-flex items-center justify-center w-20 h-20 bg-gradient-to-br from-green-500 to-emerald-600 rounded-full mb-6 shadow-lg">
              <CheckCircle2 className="w-10 h-10 text-white" />
            </div>
            
            <h1 className="text-3xl font-bold text-white mb-3">Account Created!</h1>
            <p className="text-slate-300 mb-8">
              Your account has been successfully created. You can now sign in to access your dashboard.
            </p>
            
            <button
              onClick={() => window.location.href = '/login'}
              className="w-full py-3.5 px-4 bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700 
                text-white font-semibold rounded-xl shadow-lg hover:shadow-xl 
                focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:ring-offset-2 focus:ring-offset-slate-800
                transition-all duration-200 transform hover:scale-[1.02] active:scale-[0.98]"
            >
              Go to Sign In
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 flex items-center justify-center p-4">
      {/* Subtle animated background */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-blue-500/5 rounded-full blur-3xl animate-pulse"></div>
        <div className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-purple-500/5 rounded-full blur-3xl animate-pulse" style={{ animationDelay: '1s' }}></div>
      </div>

      {/* Sign Up Card */}
      <div className="relative w-full max-w-md">
        {/* Card glow effect */}
        <div className="absolute inset-0 bg-gradient-to-r from-blue-500/20 to-purple-500/20 rounded-2xl blur-xl"></div>
        
        <div className="relative bg-slate-800/90 backdrop-blur-xl rounded-2xl shadow-2xl border border-slate-700/50 p-8">
          {/* Header */}
          <div className="text-center mb-8">
            <div className="inline-flex items-center justify-center w-16 h-16 bg-gradient-to-br from-blue-500 to-purple-600 rounded-2xl mb-4 shadow-lg">
              <User className="w-8 h-8 text-white" />
            </div>
            <h1 className="text-3xl font-bold text-white mb-2">Create Account</h1>
            <p className="text-slate-400">Join us to start managing your operations</p>
          </div>

          {/* Sign Up Form */}
          <form onSubmit={handleSubmit} noValidate className="space-y-5">
            {/* Full Name Field */}
            <div>
              <label htmlFor="name" className="block text-sm font-medium text-slate-300 mb-2">
                Full Name
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                  <User className={`w-5 h-5 transition-colors ${
                    focusedField === 'name' ? 'text-blue-400' : 'text-slate-500'
                  }`} />
                </div>
                <input
                  ref={nameRef}
                  type="text"
                  id="name"
                  name="name"
                  value={formData.name}
                  onChange={handleChange}
                  onBlur={handleBlur}
                  onFocus={() => handleFocus('name')}
                  onKeyDown={handleKeyDown}
                  disabled={isLoading}
                  autoComplete="name"
                  className={`w-full pl-12 pr-4 py-3.5 bg-slate-900/50 border rounded-xl text-white placeholder-slate-500 
                    focus:outline-none focus:ring-2 transition-all duration-200
                    disabled:opacity-50 disabled:cursor-not-allowed
                    ${touched.name && errors.name 
                      ? 'border-red-500 focus:border-red-500 focus:ring-red-500/20' 
                      : 'border-slate-600 focus:border-blue-500 focus:ring-blue-500/20'
                    }`}
                  placeholder="Enter your full name"
                  aria-invalid={touched.name && errors.name ? 'true' : 'false'}
                  aria-describedby={touched.name && errors.name ? 'name-error' : undefined}
                />
                {touched.name && errors.name && (
                  <div className="absolute inset-y-0 right-0 pr-4 flex items-center pointer-events-none">
                    <AlertCircle className="w-5 h-5 text-red-500" />
                  </div>
                )}
              </div>
              {touched.name && errors.name && (
                <p id="name-error" className="mt-2 text-sm text-red-400 flex items-center gap-1.5">
                  <AlertCircle className="w-4 h-4" />
                  {errors.name}
                </p>
              )}
            </div>

            {/* Email Field */}
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-slate-300 mb-2">
                Email Address
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                  <Mail className={`w-5 h-5 transition-colors ${
                    focusedField === 'email' ? 'text-blue-400' : 'text-slate-500'
                  }`} />
                </div>
                <input
                  type="email"
                  id="email"
                  name="email"
                  value={formData.email}
                  onChange={handleChange}
                  onBlur={handleBlur}
                  onFocus={() => handleFocus('email')}
                  onKeyDown={handleKeyDown}
                  disabled={isLoading}
                  autoComplete="email"
                  className={`w-full pl-12 pr-4 py-3.5 bg-slate-900/50 border rounded-xl text-white placeholder-slate-500 
                    focus:outline-none focus:ring-2 transition-all duration-200
                    disabled:opacity-50 disabled:cursor-not-allowed
                    ${touched.email && errors.email 
                      ? 'border-red-500 focus:border-red-500 focus:ring-red-500/20' 
                      : 'border-slate-600 focus:border-blue-500 focus:ring-blue-500/20'
                    }`}
                  placeholder="Enter your email"
                  aria-invalid={touched.email && errors.email ? 'true' : 'false'}
                  aria-describedby={touched.email && errors.email ? 'email-error' : undefined}
                />
                {touched.email && errors.email && (
                  <div className="absolute inset-y-0 right-0 pr-4 flex items-center pointer-events-none">
                    <AlertCircle className="w-5 h-5 text-red-500" />
                  </div>
                )}
              </div>
              {touched.email && errors.email && (
                <p id="email-error" className="mt-2 text-sm text-red-400 flex items-center gap-1.5">
                  <AlertCircle className="w-4 h-4" />
                  {errors.email}
                </p>
              )}
            </div>

            {/* Company Selection */}
            <div>
              <label className="block text-sm font-medium text-slate-300 mb-3">
                Company
              </label>
              <div className="space-y-3">
                <label className="flex items-center gap-3 cursor-pointer group">
                  <input
                    type="radio"
                    checked={isNewCompany}
                    onChange={() => setIsNewCompany(true)}
                    disabled={isLoading}
                    className="w-4 h-4 text-blue-500 border-slate-600 bg-slate-900/50 focus:ring-2 focus:ring-blue-500/20 cursor-pointer"
                  />
                  <span className="text-slate-300 group-hover:text-white transition-colors">
                    Create new company
                  </span>
                </label>
                {companies.length > 0 && (
                  <label className="flex items-center gap-3 cursor-pointer group">
                    <input
                      type="radio"
                      checked={!isNewCompany}
                      onChange={() => setIsNewCompany(false)}
                      disabled={isLoading}
                      className="w-4 h-4 text-blue-500 border-slate-600 bg-slate-900/50 focus:ring-2 focus:ring-blue-500/20 cursor-pointer"
                    />
                    <span className="text-slate-300 group-hover:text-white transition-colors">
                      Join existing company
                    </span>
                  </label>
                )}
              </div>
            </div>

            {/* Company Name or Selection */}
            {isNewCompany ? (
              <div>
                <label htmlFor="companyName" className="block text-sm font-medium text-slate-300 mb-2">
                  Company Name
                </label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                    <Building2 className={`w-5 h-5 transition-colors ${
                      focusedField === 'companyName' ? 'text-blue-400' : 'text-slate-500'
                    }`} />
                  </div>
                  <input
                    type="text"
                    id="companyName"
                    name="companyName"
                    value={formData.companyName}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    onFocus={() => handleFocus('companyName')}
                    onKeyDown={handleKeyDown}
                    disabled={isLoading}
                    autoComplete="organization"
                    className={`w-full pl-12 pr-4 py-3.5 bg-slate-900/50 border rounded-xl text-white placeholder-slate-500 
                      focus:outline-none focus:ring-2 transition-all duration-200
                      disabled:opacity-50 disabled:cursor-not-allowed
                      ${touched.companyName && errors.companyName 
                        ? 'border-red-500 focus:border-red-500 focus:ring-red-500/20' 
                        : 'border-slate-600 focus:border-blue-500 focus:ring-blue-500/20'
                      }`}
                    placeholder="Enter company name"
                    aria-invalid={touched.companyName && errors.companyName ? 'true' : 'false'}
                    aria-describedby={touched.companyName && errors.companyName ? 'companyName-error' : undefined}
                  />
                  {touched.companyName && errors.companyName && (
                    <div className="absolute inset-y-0 right-0 pr-4 flex items-center pointer-events-none">
                      <AlertCircle className="w-5 h-5 text-red-500" />
                    </div>
                  )}
                </div>
                {touched.companyName && errors.companyName && (
                  <p id="companyName-error" className="mt-2 text-sm text-red-400 flex items-center gap-1.5">
                    <AlertCircle className="w-4 h-4" />
                    {errors.companyName}
                  </p>
                )}
              </div>
            ) : (
              <div>
                <label htmlFor="existingCompanyId" className="block text-sm font-medium text-slate-300 mb-2">
                  Select Company
                </label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                    <Building2 className="w-5 h-5 text-slate-500" />
                  </div>
                  <select
                    id="existingCompanyId"
                    name="existingCompanyId"
                    value={formData.existingCompanyId || ''}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    disabled={isLoading}
                    className="w-full pl-12 pr-4 py-3.5 bg-slate-900/50 border border-slate-600 rounded-xl text-white 
                      focus:outline-none focus:ring-2 focus:border-blue-500 focus:ring-blue-500/20 transition-all duration-200
                      disabled:opacity-50 disabled:cursor-not-allowed appearance-none"
                  >
                    <option value="">Select a company</option>
                    {companies.map(company => (
                      <option key={company.id} value={company.id}>
                        {company.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
            )}

            {/* Password Field */}
            <div>
              <label htmlFor="password" className="block text-sm font-medium text-slate-300 mb-2">
                Password
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                  <Lock className={`w-5 h-5 transition-colors ${
                    focusedField === 'password' ? 'text-blue-400' : 'text-slate-500'
                  }`} />
                </div>
                <input
                  type={showPassword ? 'text' : 'password'}
                  id="password"
                  name="password"
                  value={formData.password}
                  onChange={handleChange}
                  onBlur={handleBlur}
                  onFocus={() => handleFocus('password')}
                  onKeyDown={handleKeyDown}
                  disabled={isLoading}
                  autoComplete="new-password"
                  className={`w-full pl-12 pr-12 py-3.5 bg-slate-900/50 border rounded-xl text-white placeholder-slate-500 
                    focus:outline-none focus:ring-2 transition-all duration-200
                    disabled:opacity-50 disabled:cursor-not-allowed
                    ${touched.password && errors.password 
                      ? 'border-red-500 focus:border-red-500 focus:ring-red-500/20' 
                      : 'border-slate-600 focus:border-blue-500 focus:ring-blue-500/20'
                    }`}
                  placeholder="Create a strong password"
                  aria-invalid={touched.password && errors.password ? 'true' : 'false'}
                  aria-describedby={touched.password && errors.password ? 'password-error' : undefined}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  disabled={isLoading}
                  className="absolute inset-y-0 right-0 pr-4 flex items-center text-slate-400 hover:text-slate-300 transition-colors disabled:opacity-50"
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? (
                    <EyeOff className="w-5 h-5" />
                  ) : (
                    <Eye className="w-5 h-5" />
                  )}
                </button>
              </div>
              {touched.password && errors.password && (
                <p id="password-error" className="mt-2 text-sm text-red-400 flex items-center gap-1.5">
                  <AlertCircle className="w-4 h-4" />
                  {errors.password}
                </p>
              )}
              {!errors.password && (
                <p className="mt-2 text-xs text-slate-400">
                  Must be 8+ characters with uppercase, lowercase, and number
                </p>
              )}
            </div>

            {/* Confirm Password Field */}
            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-slate-300 mb-2">
                Confirm Password
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                  <Lock className={`w-5 h-5 transition-colors ${
                    focusedField === 'confirmPassword' ? 'text-blue-400' : 'text-slate-500'
                  }`} />
                </div>
                <input
                  type={showConfirmPassword ? 'text' : 'password'}
                  id="confirmPassword"
                  name="confirmPassword"
                  value={formData.confirmPassword}
                  onChange={handleChange}
                  onBlur={handleBlur}
                  onFocus={() => handleFocus('confirmPassword')}
                  onKeyDown={handleKeyDown}
                  disabled={isLoading}
                  autoComplete="new-password"
                  className={`w-full pl-12 pr-12 py-3.5 bg-slate-900/50 border rounded-xl text-white placeholder-slate-500 
                    focus:outline-none focus:ring-2 transition-all duration-200
                    disabled:opacity-50 disabled:cursor-not-allowed
                    ${touched.confirmPassword && errors.confirmPassword 
                      ? 'border-red-500 focus:border-red-500 focus:ring-red-500/20' 
                      : 'border-slate-600 focus:border-blue-500 focus:ring-blue-500/20'
                    }`}
                  placeholder="Re-enter your password"
                  aria-invalid={touched.confirmPassword && errors.confirmPassword ? 'true' : 'false'}
                  aria-describedby={touched.confirmPassword && errors.confirmPassword ? 'confirmPassword-error' : undefined}
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  disabled={isLoading}
                  className="absolute inset-y-0 right-0 pr-4 flex items-center text-slate-400 hover:text-slate-300 transition-colors disabled:opacity-50"
                  aria-label={showConfirmPassword ? 'Hide password' : 'Show password'}
                >
                  {showConfirmPassword ? (
                    <EyeOff className="w-5 h-5" />
                  ) : (
                    <Eye className="w-5 h-5" />
                  )}
                </button>
              </div>
              {touched.confirmPassword && errors.confirmPassword && (
                <p id="confirmPassword-error" className="mt-2 text-sm text-red-400 flex items-center gap-1.5">
                  <AlertCircle className="w-4 h-4" />
                  {errors.confirmPassword}
                </p>
              )}
            </div>

            {/* Terms and Conditions */}
            <div className="pt-1">
              <label className="flex items-start gap-3 cursor-pointer group">
                <input
                  type="checkbox"
                  required
                  className="w-4 h-4 mt-1 rounded border-slate-600 bg-slate-900/50 text-blue-500 
                    focus:ring-2 focus:ring-blue-500/20 focus:ring-offset-0 transition-colors cursor-pointer"
                  disabled={isLoading}
                />
                <span className="text-sm text-slate-400 group-hover:text-slate-300 transition-colors">
                  I agree to the{' '}
                  <button type="button" className="text-blue-400 hover:text-blue-300 underline">
                    Terms of Service
                  </button>
                  {' '}and{' '}
                  <button type="button" className="text-blue-400 hover:text-blue-300 underline">
                    Privacy Policy
                  </button>
                </span>
              </label>
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              disabled={isLoading || !isFormValid}
              className="w-full py-3.5 px-4 bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700 
                text-white font-semibold rounded-xl shadow-lg hover:shadow-xl 
                focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:ring-offset-2 focus:ring-offset-slate-800
                transition-all duration-200 transform hover:scale-[1.02] active:scale-[0.98]
                disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none disabled:hover:scale-100
                flex items-center justify-center gap-2"
            >
              {isLoading ? (
                <>
                  <Loader2 className="w-5 h-5 animate-spin" />
                  <span>Creating account...</span>
                </>
              ) : (
                <span>Create Account</span>
              )}
            </button>
          </form>

          {/* Sign In Link */}
          <div className="mt-6 text-center">
            <p className="text-sm text-slate-400">
              Already have an account?{' '}
              <button
                type="button"
                onClick={() => window.location.href = '/login'}
                className="text-blue-400 hover:text-blue-300 font-medium transition-colors focus:outline-none focus:underline"
                disabled={isLoading}
              >
                Sign in
              </button>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
