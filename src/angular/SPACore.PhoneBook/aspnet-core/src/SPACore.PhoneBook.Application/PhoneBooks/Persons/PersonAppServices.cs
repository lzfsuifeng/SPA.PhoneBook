﻿using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.AutoMapper;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using SPACore.PhoneBook.Enums;
using SPACore.PhoneBook.PhoneBooks.Persons.Authorization;
using SPACore.PhoneBook.PhoneBooks.Persons.Dtos;
using SPACore.PhoneBook.PhoneBooks.PhoneNumbers;
using SPACore.PhoneBook.PhoneBooks.PhoneNumbers.Dtos;

namespace SPACore.PhoneBook.PhoneBooks.Persons
{
    /// <summary>
    /// Person应用层服务的接口实现方法
    /// </summary>
    [AbpAuthorize(PersonAppPermissions.Person)]
    public class PersonAppService : PhoneBookAppServiceBase, IPersonAppService
    {
        ////BCC/ BEGIN CUSTOM CODE SECTION
        ////ECC/ END CUSTOM CODE SECTION
        private readonly IRepository<Person, int> _personRepository;
        private readonly IRepository<PhoneNumber, long> _phoneNumbeRepository;

        private readonly IEnumAppService _enumAppService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public PersonAppService(IRepository<Person, int> personRepository, IEnumAppService enumAppService, IRepository<PhoneNumber, long> phoneNumbeRepository)
        {
            _personRepository = personRepository;
            _enumAppService = enumAppService;
            _phoneNumbeRepository = phoneNumbeRepository;
        }

        /// <summary>
        /// 获取Person的分页列表信息
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<PagedResultDto<PersonListDto>> GetPagedPersons(GetPersonsInput input)
        {

 
            var query = _personRepository.GetAll().Include(a => a.PhoneNumbers).WhereIf(!input.Filter.IsNullOrEmpty(),
                p => p.Name.Contains(input.Filter) ||
                     p.Address.Contains(input.Filter) ||
                     p.EmailAddress.Contains(input.Filter));
            //TODO:根据传入的参数添加过滤条件
            var personCount = await query.CountAsync();

            var persons = await query
                .OrderBy(input.Sorting).AsNoTracking()
                .PageBy(input)
                .ToListAsync();

            //var personListDtos = ObjectMapper.Map<List <PersonListDto>>(persons);
            var personListDtos = persons.MapTo<List<PersonListDto>>();

            return new PagedResultDto<PersonListDto>(
                personCount,
                personListDtos
                );

        }

        /// <summary>
        /// 通过指定id获取PersonListDto信息
        /// </summary>
        public async Task<PersonListDto> GetPersonByIdAsync(EntityDto<int> input)
        {
            var person = await _personRepository.GetAllIncluding(a => a.PhoneNumbers).FirstOrDefaultAsync(a => a.Id == input.Id);

       var dto=     ObjectMapper.Map<PersonListDto>(person);


            return dto;
        }

        /// <summary>
        /// 导出Person为excel表
        /// </summary>
        /// <returns></returns>
        //public async Task<FileDto> GetPersonsToExcel(){
        //var users = await UserManager.Users.ToListAsync();
        //var userListDtos = ObjectMapper.Map<List<UserListDto>>(users);
        //await FillRoleNames(userListDtos);
        //return _userListExcelExporter.ExportToFile(userListDtos);
        //}
        /// <summary>
        /// MPA版本才会用到的方法
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<GetPersonForEditOutput> GetPersonForEdit(NullableIdDto<int> input)
        {
            var output = new GetPersonForEditOutput();
            PersonEditDto personEditDto;

            if (input.Id.HasValue)
            {
                var entity = await _personRepository.GetAllIncluding(a => a.PhoneNumbers).FirstOrDefaultAsync(a => a.Id == input.Id.Value);
                
                personEditDto = ObjectMapper.Map<PersonEditDto>(entity);
            }
            else
            {
                personEditDto = new PersonEditDto();
            }
            
            output.Person = personEditDto;


     //       IsSelected = userMarginsEditDto.Operationdescribes.ToDescription() == a.Key,

            

            //output.PhoneNumberType = _enumAppService.GetPhoneNumberTypeList().Select(a=>new ComboboxItemDto()
            //{
            //    DisplayText = a.Key,
            //    Value = a.Value,
            //    IsSelected = personEditDto.
            //});



            return output;

        }

        /// <summary>
        /// 添加或者修改Person的公共方法
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task CreateOrUpdatePerson(CreateOrUpdatePersonInput input)
        {

            if (input.Person.Id.HasValue)
            {
                await UpdatePersonAsync(input.Person);
            }
            else
            {
                await CreatePersonAsync(input.Person);
            }
        }

        /// <summary>
        /// 新增Person
        /// </summary>
        [AbpAuthorize(PersonAppPermissions.Person_CreatePerson)]
        protected virtual async Task<PersonEditDto> CreatePersonAsync(PersonEditDto input)
        {
            //TODO:新增前的逻辑判断，是否允许新增
            var entity = ObjectMapper.Map<Person>(input);

            entity = await _personRepository.InsertAsync(entity);

            var dto = ObjectMapper.Map<PersonEditDto>(entity);

            return dto;
        }

        /// <summary>
        /// 编辑Person
        /// </summary>
        [AbpAuthorize(PersonAppPermissions.Person_EditPerson)]
        protected virtual async Task UpdatePersonAsync(PersonEditDto input)
        {
            //TODO:更新前的逻辑判断，是否允许更新
            var entity = await _personRepository.GetAsync(input.Id.Value);
          //  input.MapTo(entity);

         ObjectMapper.Map(input, entity);

            await _personRepository.UpdateAsync(entity);
        }

        /// <summary>
        /// 删除Person信息的方法
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [AbpAuthorize(PersonAppPermissions.Person_DeletePerson)]
        public async Task DeletePerson(EntityDto<int> input)
        {

            //TODO:删除前的逻辑判断，是否允许删除
            await _personRepository.DeleteAsync(input.Id);
        }

        /// <summary>
        /// 批量删除Person的方法
        /// </summary>
        [AbpAuthorize(PersonAppPermissions.Person_BatchDeletePersons)]
        public async Task BatchDeletePersonsAsync(List<int> input)
        {
            //TODO:批量删除前的逻辑判断，是否允许删除
            await _personRepository.DeleteAsync(s => input.Contains(s.Id));
        }

        #region 电话有关的逻辑
        public async Task DeletePhoneAsync(EntityDto<long> input)
        {
     await       _phoneNumbeRepository.DeleteAsync(input.Id);
         }

        public async Task<PhoneNumberListDto> AddPhone(PhoneNumberEditDto input)
        {
            var person = _personRepository.Get(input.PersonId);
            await _personRepository.EnsureCollectionLoadedAsync(person, p => p.PhoneNumbers);

            var phoneNumber = ObjectMapper.Map<PhoneNumber>(input);
            person.PhoneNumbers.Add(phoneNumber);
          //  通过保存到数据库来获取新手机号码自增的ID
             await CurrentUnitOfWork.SaveChangesAsync();

            return ObjectMapper.Map<PhoneNumberListDto>(phoneNumber);
        }



        #endregion

    }
}
