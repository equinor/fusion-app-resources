import { useCallback } from 'react';
import Person from '../../../../../../../models/Person';
import useForm from '../../../../../../../hooks/useForm';
import { v1 as uuid } from 'uuid';

const createDefaultState = (): Person[] => ([
  {
    personnelId: uuid(),
    name: "",
    firstName: "",
    lastName: "",
    phoneNumber: "",
    mail: "",
    jobTitle: ""
  }]);

const useAddPersonnelForm = (defaultState?: Person[] | null) => {
  const validateForm = useCallback((formState: Person[]) => {
    return !formState
      .some(p =>
        !Boolean(
          p.firstName &&
          p.lastName &&
          p.phoneNumber &&
          p.mail))
  }, []);

  return useForm(createDefaultState, validateForm, defaultState);
};

export default useAddPersonnelForm;
